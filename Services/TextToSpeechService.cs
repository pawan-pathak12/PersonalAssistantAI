using System.Speech.Synthesis;

namespace PersonalAssistantAI.Services
{
    public class TextToSpeechService : IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;
        private bool _enabled = true;

        public bool IsSpeaking { get; private set; }

        public TextToSpeechService(string? preferredVoice = null)
        {
            _synthesizer = new SpeechSynthesizer();
            _synthesizer.SetOutputToDefaultAudioDevice();

            // Select a voice if available, otherwise use default
            if (!string.IsNullOrEmpty(preferredVoice) &&
                _synthesizer.GetInstalledVoices().Any(v => v.VoiceInfo.Name == preferredVoice))
            {
                _synthesizer.SelectVoice(preferredVoice);
            }
            else
            {
                _synthesizer.SelectVoice("Microsoft David Desktop");
            }

            // Customize speaking behavior
            _synthesizer.Rate = 1;    // Speed (-10 to +10)
            _synthesizer.Volume = 95; // Volume (0–100)

            // Register event once
            _synthesizer.SpeakCompleted += (s, e) => { IsSpeaking = false; };
        }

        /// <summary>
        /// Speaks text asynchronously (non-blocking)
        /// </summary>
        public async Task Speak(string text)
        {
            if (!_enabled || string.IsNullOrWhiteSpace(text))
                return;

            Stop(); // Ensure previous speech stops

            IsSpeaking = true;
            var cleanText = CleanTextForSpeech(text);


            await Task.Run(() =>
            {
                try
                {
                    _synthesizer.SpeakAsync(cleanText);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TTS Error: {ex.Message}");
                }
                finally
                {
                    _synthesizer.SpeakCompleted += (sender, e) =>
                    {
                        IsSpeaking = false; // ← ADD THIS
                    };
                }
            });
        }

        /// <summary>
        /// Instantly stop any ongoing speech (barge-in support)
        /// </summary>
        public void Stop()
        {
            try
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            catch { /* ignore */ }
            IsSpeaking = false;
        }

        /// <summary>
        /// Toggles whether speech is enabled
        /// </summary>
        public void Toggle()
        {
            _enabled = !_enabled;
            Console.WriteLine($"Voice responses: {(_enabled ? "ON" : "OFF")}");
        }

        public bool IsEnabled => _enabled;

        /// <summary>
        /// Removes unwanted formatting before speaking
        /// </summary>
        private static string CleanTextForSpeech(string text)
        {
            return text
                .Replace("**", "")
                .Replace("__", "")
                .Replace("*", "")
                .Replace("_", "")
                .Replace("#", "")
                .Replace("-", "")
                .Replace("[[SEARCH:", "Searching for ")
                .Replace("]]", "")
                .Replace("<", "")
                .Replace(">", "")
                .Trim();
        }

        public void Dispose()
        {
            Stop();
            _synthesizer.Dispose();
        }
    }
}
