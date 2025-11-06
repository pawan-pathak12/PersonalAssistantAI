using NAudio.Wave;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PersonalAssistantAI.Services
{
    /// <summary>
    /// Continuously listens through the microphone using NAudio,
    /// detects when you're speaking (using RMS-based voice activity detection),
    /// saves that audio to a file, sends it to Whisper CLI for transcription,
    /// and returns recognized text through a callback.
    /// </summary>
    public class NAudioVoiceService : IDisposable
    {

        #region Private variable 
        // Paths to Whisper executable and model
        private readonly string _whisperPath = @"C:\whisper\Release\whisper-cli.exe";
        private readonly string _modelPath = @"C:\whisper\models\ggml-small.en.bin";



        // Callback: called when Whisper returns text
        private readonly Func<string, Task> _onFinalText;

        // Optional callback: triggered when user interrupts while TTS is speaking
        //  private readonly Action? _onBargeIn;
        private readonly TextToSpeechService _ttsService; // <--- ADD THIS

        // NAudio objects
        private WaveInEvent? _waveIn;     // Microphone capture
        private volatile bool _listening; // Are we currently listening?
        private volatile bool _inSpeech;  // Is speech currently happening?

        // Timing for VAD
        private DateTime _speechStartUtc;
        private DateTime _lastVoiceUtc;

        private MemoryStream? _currentUtterance; // Stores current speech audio

        // Voice activity detection (VAD) parameters
        private const double VadThresholdRms = 0.02; // Loudness threshold
        private static readonly TimeSpan SilenceHangover = TimeSpan.FromMilliseconds(450); // End silence
        private static readonly TimeSpan MinUtterance = TimeSpan.FromMilliseconds(350);    // Too short discard
        private static readonly TimeSpan MaxUtterance = TimeSpan.FromSeconds(15);          // Max utterance time

        // Thread-safe audio segment queue for Whisper
        private readonly ConcurrentQueue<byte[]> _segments = new();
        private readonly SemaphoreSlim _segmentSignal = new(0);
        private CancellationTokenSource? _cts;
        private Task? _workerTask;

        #endregion

        // Constructor — gets callbacks for transcription and barge-in
        public NAudioVoiceService(Func<string, Task> onFinalText, TextToSpeechService textToSpeechService)
        {
            _onFinalText = onFinalText;
            _ttsService = textToSpeechService;

        }

        // 🚀 Start listening
        public void Start(int deviceNumber = -1)
        {
            if (_listening) return;

            // Setup mic input format (16kHz mono)
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 50
            };

            // Hook up mic events
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            // Start background transcription worker
            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(() => TranscribeLoopAsync(_cts.Token));

            _currentUtterance = null;
            _inSpeech = false;
            _listening = true;

            // Start mic recording
            _waveIn.StartRecording();
            Console.WriteLine(" Always-listening started (NAudio + VAD). Speak anytime.");
        }

        // 🛑 Stop listening
        public void Stop()
        {
            if (!_listening) return;
            _listening = false;

            // Stop mic
            try { _waveIn?.StopRecording(); } catch { }
            _waveIn?.Dispose();
            _waveIn = null;

            // Stop background worker
            _cts?.Cancel();
            try { _workerTask?.Wait(1000); } catch { }
            _workerTask = null;
            _cts?.Dispose();
            _cts = null;
        }

        // 🎤 Called every 50ms with new audio data
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_listening || e.BytesRecorded <= 0) return;

            if (_ttsService.IsSpeaking)
            {
                // Option 1 (Simple Mute): Just return and ignore the audio segment.
                return;
            }
            // Compute loudness
            double rms = ComputeRms16(e.Buffer, e.BytesRecorded);
            var now = DateTime.UtcNow;

            // Speech start detected
            if (!_inSpeech && rms >= VadThresholdRms)
            {
                _inSpeech = true;
                _speechStartUtc = now;
                _lastVoiceUtc = now;
                _currentUtterance = new MemoryStream(capacity: e.BytesRecorded * 8);

                // If speaking over AI speech, trigger "barge-in" callback


                Console.WriteLine(" Speech detected: recording what user speak..…");
            }

            // Still speaking — collect audio samples
            if (_inSpeech && _currentUtterance != null)
            {
                _currentUtterance.Write(e.Buffer, 0, e.BytesRecorded);
                if (rms >= VadThresholdRms) _lastVoiceUtc = now;

                var speechDuration = now - _speechStartUtc;
                var silenceDuration = now - _lastVoiceUtc;

                // Stop if too long or silence detected
                if (speechDuration > MaxUtterance || silenceDuration > SilenceHangover)
                    FinishUtterance(now);
            }
        }

        // 🧩 Called when a speech segment finishes
        private void FinishUtterance(DateTime now)
        {
            _inSpeech = false;
            if (_currentUtterance == null) return;

            var duration = now - _speechStartUtc;
            if (duration < MinUtterance || _currentUtterance.Length < 1600)
            {
                // Too short → discard
                _currentUtterance.Dispose();
                _currentUtterance = null;
                Console.WriteLine(" Utterance discarded (too short).");
                Console.WriteLine();
                return;
            }

            // Convert recorded bytes to PCM array
            var pcm = _currentUtterance.ToArray();
            _currentUtterance.Dispose();
            _currentUtterance = null;

            // Queue for Whisper transcription
            _segments.Enqueue(pcm);
            _segmentSignal.Release();
            Console.WriteLine($"user speaking word queued ({duration.TotalSeconds:F1}s, {pcm.Length / 2} samples).");
            Console.WriteLine();
        }

        // 🧠 Background task: process queued segments through Whisper CLI
        private async Task TranscribeLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await _segmentSignal.WaitAsync(ct); }
                catch (OperationCanceledException) { break; }

                while (_segments.TryDequeue(out var pcm))
                {
                    try
                    {
                        // Write to temporary .wav file
                        var wavPath = Path.Combine(Path.GetTempPath(), $"jarvis_{Guid.NewGuid():N}.wav");
                        WriteWav16kMono(wavPath, pcm);

                        // Setup Whisper CLI process
                        var psi = new ProcessStartInfo
                        {
                            FileName = _whisperPath,
                            Arguments = $"-m \"{_modelPath}\" -f \"{wavPath}\" --no-timestamps -l en",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using var p = Process.Start(psi)!;
                        string stdout = await p.StandardOutput.ReadToEndAsync();
                        string stderr = await p.StandardError.ReadToEndAsync();
                        p.WaitForExit();

                        try { File.Delete(wavPath); } catch { }

                        if (p.ExitCode != 0)
                        {
                            Console.WriteLine($"Whisper error (code {p.ExitCode}): {stderr}");
                            continue;
                        }
                        //debuging : code is not triggered from here 


                        // Clean and return text
                        var text = CleanOutput(stdout);
                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"You said: {text}");
                            Console.ResetColor();

                            Console.WriteLine($"[DEBUG] Sending text to AI: {text}");

                            await _onFinalText(text);
                        }
                        else
                        {
                            Console.WriteLine("No speech detected in utterance.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Transcribe error: {ex.Message}");
                    }
                }
            }
        }

        // 🔊 Compute loudness (RMS) of 16-bit PCM buffer
        private static double ComputeRms16(byte[] buffer, int bytes)
        {
            int samples = bytes / 2;
            if (samples == 0) return 0;

            double sumSq = 0;
            for (int i = 0; i < bytes; i += 2)
            {
                short s = (short)(buffer[i] | (buffer[i + 1] << 8));
                double n = s / 32768.0;
                sumSq += n * n;
            }
            return Math.Sqrt(sumSq / samples);
        }

        // 💾 Write PCM data to WAV file (16kHz mono)
        private static void WriteWav16kMono(string path, byte[] pcm16)
        {
            using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var w = new WaveFileWriter(fs, new WaveFormat(16000, 16, 1));
            w.Write(pcm16, 0, pcm16.Length);
        }

        // 🧹 Clean Whisper output text
        private static string CleanOutput(string output)
        {
            return output
                .Replace("whisper", "", StringComparison.OrdinalIgnoreCase)
                .Replace("model", "", StringComparison.OrdinalIgnoreCase)
                .Replace("\r", "")
                .Replace("\n", " ")
                .Trim();
        }

        // ⚠️ When mic recording stops
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Console.WriteLine($"Mic stopped due to error: {e.Exception.Message}");
            else
                Console.WriteLine("Mic stopped.");

            if (_inSpeech) FinishUtterance(DateTime.UtcNow);
        }

        // ♻️ Cleanup resources
        public void Dispose()
        {
            Stop();
            _segmentSignal?.Dispose();
        }
    }
}
