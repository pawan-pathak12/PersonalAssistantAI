using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class ChatLoopService
    {
        private static int _accessGranted = 0;
        private static string? _expectedPwd;
        private static bool _pwPromptSpoken;
        public static async Task ChatLoop(
        ChatHistory history,
        Kernel kernel,
        OpenAIPromptExecutionSettings exec,
        WebSearchService webSearch,
        TextToSpeechService ttsService)
        {
            int empty = 0;

            while (true)
            {
                #region Password Check
                // Password gate for typed input
                if (System.Threading.Volatile.Read(ref _accessGranted) == 0)
                {
                    if (ttsService.IsSpeaking) ttsService.Stop();
                    Console.WriteLine();

                    #region Authentication: Password Prompt (recommended)
                    if (!_pwPromptSpoken)
                    {
                        if (ttsService.IsSpeaking) ttsService.Stop(); // avoid overlap
                        ttsService.Speak("Please enter your password to authenticate.");
                        _pwPromptSpoken = true;
                    }

                    #endregion

                    Console.Write("Password > ");
                    var pwdInput = (Console.ReadLine() ?? string.Empty).Trim();

                    if (pwdInput.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                    pwdInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (string.IsNullOrWhiteSpace(pwdInput))
                        continue;

                    var candidate = Normalize(pwdInput);
                    continue; // stay in gate until unlocked

                }

                #endregion
                try
                {
                    Console.WriteLine();
                    Console.Write("User > ");
                    var input = Console.ReadLine()?.Trim() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        if (++empty >= 3) break;
                        Console.WriteLine("Say something or type a message...");
                        continue;
                    }

                    empty = 0;

                    if (input.Equals("q", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (input.Equals("voice", StringComparison.OrdinalIgnoreCase))
                    {
                        ttsService.Toggle();
                        continue;
                    }

                    // Barge-in for typed input: stop speaking immediately
                    if (ttsService.IsSpeaking) ttsService.Stop();

                    await ProcessMessageService.ProcessMessageAsync(input, history, kernel, exec, webSearch, ttsService);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in chat loop: {ex.Message}");
                }
            }
        }

        #region Helper method for Passwod Check 
        private static string Normalize(string s)
    => new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static bool IsPasswordMatch(string candidate, string expected)
        => candidate == expected;
        #endregion
    }
}
