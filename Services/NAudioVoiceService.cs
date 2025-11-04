using NAudio.Wave;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PersonalAssistantAI.Services
{
    public class NAudioVoiceService : IDisposable
    {
        private readonly string _whisperPath = @"C:\whisper\Release\whisper-cli.exe";
        private readonly string _modelPath = @"C:\whisper\models\ggml-tiny.en.bin";

        private readonly Func<string, Task> _onFinalText;
        private readonly Action? _onBargeIn; // NEW

        private WaveInEvent? _waveIn;
        private volatile bool _listening;
        private volatile bool _inSpeech;
        private DateTime _speechStartUtc;
        private DateTime _lastVoiceUtc;
        private MemoryStream? _currentUtterance;

        private const double VadThresholdRms = 0.02;
        private static readonly TimeSpan SilenceHangover = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan MinUtterance = TimeSpan.FromMilliseconds(350);
        private static readonly TimeSpan MaxUtterance = TimeSpan.FromSeconds(15);

        private readonly ConcurrentQueue<byte[]> _segments = new();
        private readonly SemaphoreSlim _segmentSignal = new(0);
        private CancellationTokenSource? _cts;
        private Task? _workerTask;

        public NAudioVoiceService(Func<string, Task> onFinalText, Action? onBargeIn = null)
        {
            _onFinalText = onFinalText;
            _onBargeIn = onBargeIn;
        }

        public void Start(int deviceNumber = -1)
        {
            if (_listening) return;


            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(16000, 16, 1),
                BufferMilliseconds = 50
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(() => TranscribeLoopAsync(_cts.Token));

            _currentUtterance = null;
            _inSpeech = false;
            _listening = true;
            _waveIn.StartRecording();

            Console.WriteLine(" Always-listening started (NAudio + VAD). Speak anytime.");
        }

        public void Stop()
        {
            if (!_listening) return;
            _listening = false;

            try { _waveIn?.StopRecording(); } catch { }
            _waveIn?.Dispose();
            _waveIn = null;

            _cts?.Cancel();
            try { _workerTask?.Wait(1000); } catch { }
            _workerTask = null;
            _cts?.Dispose();
            _cts = null;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_listening || e.BytesRecorded <= 0) return;

            double rms = ComputeRms16(e.Buffer, e.BytesRecorded);
            var now = DateTime.UtcNow;

            // Speech start
            if (!_inSpeech && rms >= VadThresholdRms)
            {
                _inSpeech = true;
                _speechStartUtc = now;
                _lastVoiceUtc = now;
                _currentUtterance = new MemoryStream(capacity: e.BytesRecorded * 8);

                // NEW: barge-in callback (stop TTS immediately)
                try { _onBargeIn?.Invoke(); } catch { /* ignore */ }

                Console.WriteLine(" Speech detected: recording utterance…");
            }

            if (_inSpeech && _currentUtterance != null)
            {
                _currentUtterance.Write(e.Buffer, 0, e.BytesRecorded);
                if (rms >= VadThresholdRms) _lastVoiceUtc = now;

                var speechDuration = now - _speechStartUtc;
                var silenceDuration = now - _lastVoiceUtc;

                if (speechDuration > MaxUtterance || silenceDuration > SilenceHangover)
                {
                    FinishUtterance(now);
                }
            }
        }

        private void FinishUtterance(DateTime now)
        {
            _inSpeech = false;

            if (_currentUtterance == null) return;

            var duration = now - _speechStartUtc;
            if (duration < MinUtterance || _currentUtterance.Length < 1600)
            {
                _currentUtterance.Dispose();
                _currentUtterance = null;
                Console.WriteLine(" Utterance discarded (too short).");
                return;
            }

            var pcm = _currentUtterance.ToArray();
            _currentUtterance.Dispose();
            _currentUtterance = null;

            _segments.Enqueue(pcm);
            _segmentSignal.Release();
            Console.WriteLine($" Utterance queued ({duration.TotalSeconds:F1}s, {pcm.Length / 2} samples).");
        }

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
                        var wavPath = Path.Combine(Path.GetTempPath(), $"jarvis_{Guid.NewGuid():N}.wav");
                        WriteWav16kMono(wavPath, pcm);

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

                        var text = CleanOutput(stdout);
                        if (!string.IsNullOrWhiteSpace(text) && text.Length > 1)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"You said: {text}");
                            Console.ResetColor();
                            await _onFinalText(text); // async callback
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

        private static void WriteWav16kMono(string path, byte[] pcm16)
        {
            using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var w = new WaveFileWriter(fs, new WaveFormat(16000, 16, 1));
            w.Write(pcm16, 0, pcm16.Length);
        }

        private static string CleanOutput(string output)
        {
            return output
                .Replace("whisper", "", StringComparison.OrdinalIgnoreCase)
                .Replace("model", "", StringComparison.OrdinalIgnoreCase)
                .Replace("\r", "")
                .Replace("\n", " ")
                .Trim();
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
                Console.WriteLine($"Mic stopped due to error: {e.Exception.Message}");
            else
                Console.WriteLine("Mic stopped.");

            if (_inSpeech) FinishUtterance(DateTime.UtcNow);
        }




        public void Dispose()
        {
            Stop();
            _segmentSignal?.Dispose();
        }
    }
}