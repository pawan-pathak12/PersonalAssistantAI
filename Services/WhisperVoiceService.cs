// Services/WhisperVoiceService.cs
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PersonalAssistantAI.Services;

public class WhisperVoiceService
{
    private readonly string _whisperPath = @"C:\whisper\Release\whisper-cli.exe";
    private readonly string _modelPath = @"C:\whisper\models\ggml-tiny.en.bin";
    private readonly string _tempWav = Path.Combine(Path.GetTempPath(), "voice_input.wav");
    private readonly Action<string> _onFinalText;
    private bool _isRecording = false;

    public WhisperVoiceService(Action<string> onFinalText)
    {
        _onFinalText = onFinalText;
    }

    public async void StartOneShot()
    {
        if (_isRecording) return;
        _isRecording = true;

        Console.Write("Listening ( sec)... ");

        try
        {
            // Record 3 seconds
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-f dshow -i audio=\"Microphone Array (Intel® Smart Sound Technology for Digital Microphones)\" -t 5 -y \"{_tempWav}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = Process.Start(ffmpeg);
            p?.WaitForExit();

            // Run Whisper
            var whisper = new ProcessStartInfo
            {
                FileName = _whisperPath,
                Arguments = $"-m \"{_modelPath}\" -f \"{_tempWav}\" --no-timestamps",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var wp = Process.Start(whisper)!;
            string output = await wp.StandardOutput.ReadToEndAsync();
            wp.WaitForExit();

            var text = CleanOutput(output);
            if (!string.IsNullOrWhiteSpace(text) && text.Length > 1)
            {
                Console.WriteLine($"\rYou said: {text}          ");
                _onFinalText(text); // Send to AI
            }
            else
            {
                Console.WriteLine("\rNo speech detected.         ");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\rError: {ex.Message}      ");
        }
        finally
        {
            _isRecording = false;
            // NO AUTO-RESTART HERE — AI will call StartOneShot() after response
        }
    }

    private string CleanOutput(string output)
    {
        return Regex.Replace(output, @"\[\d+:\d+:\d+\.\d+ -> \d+:\d+:\d+\.\d+\]\s*", "")
                    .Replace("whisper", "")
                    .Replace("model", "")
                    .Trim();
    }

    public bool IsRecording => _isRecording;
}