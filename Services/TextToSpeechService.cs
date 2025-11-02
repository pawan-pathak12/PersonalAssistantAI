using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace PersonalAssistantAI.Services;

public class TextToSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _enabled = true;
    public bool IsSpeaking { get; private set; } // ← ADD THIS

    public TextToSpeechService()
    {
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SetOutputToDefaultAudioDevice();

        // Optional: Select a specific voice
        // _synthesizer.SelectVoice("Microsoft David Desktop");
    }

    public void Speak(string text)
    {
        if (!_enabled || string.IsNullOrWhiteSpace(text)) return;

        try
        {
            IsSpeaking = true; // ← ADD THIS
            var cleanText = CleanTextForSpeech(text);
            _synthesizer.SpeakAsync(cleanText);

            // Set up event to know when speaking finishes
            _synthesizer.SpeakCompleted += (sender, e) =>
            {
                IsSpeaking = false; // ← ADD THIS
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TTS Error: {ex.Message}");
            IsSpeaking = false; // ← ADD THIS
        }
    }

    public void Toggle()
    {
        _enabled = !_enabled;
        Console.WriteLine($"Voice responses: {(_enabled ? "ON" : "OFF")}");
    }

    public bool IsEnabled => _enabled;

    private string CleanTextForSpeech(string text)
    {
        return text
            .Replace("**", "")
            .Replace("__", "")
            .Replace("*", "")
            .Replace("_", "")
            .Replace("#", "")
            .Replace("-", "")
            .Replace("[[SEARCH:", "Searching for")
            .Replace("]]", "");
    }
    private string CleanOutput(string output)
    {
        var cleaned = Regex.Replace(output, @"\[\d+:\d+:\d+\.\d+ -> \d+:\d+:\d+\.\d+\]\s*", "")
                        .Replace("whisper", "")
                        .Replace("model", "")
                        .Trim();

        // Filter out noise indicators
        if (cleaned.Contains("[MUSIC") ||
            cleaned.Contains("[BLANK") ||
            cleaned.Contains("[NOISE") ||
            cleaned.Length < 3) // Too short to be real speech
        {
            return string.Empty;
        }

        return cleaned;
    }
}