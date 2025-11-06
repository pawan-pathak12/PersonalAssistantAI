using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalAssistantAI.Plugin;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class StartChatService
    {
        private static int _accessGranted = 0;
        private static string? _expectedPwd;
        private static bool _pwPromptSpoken;
        public static async Task StartChat(Kernel kernel)
        {
            bool _entryToken;
            #region Load configuration
            var config = new ConfigurationBuilder()
                                    .SetBasePath(AppContext.BaseDirectory)
                                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                    .Build();

            var google = config.GetSection("GoogleSearch");
            var jarvis = config.GetRequiredSection("JarvisPassword");
            var apiKey = google["ApiKey"] ?? throw new InvalidOperationException("Google API key missing");
            var engineId = google["SearchEngineId"] ?? throw new InvalidOperationException("Search engine ID missing");
            var jarvisPassword = jarvis["pass"];

            #endregion

            _expectedPwd = Normalize(jarvisPassword);
            var webSearch = new WebSearchService(apiKey, engineId);
            kernel.Plugins.AddFromObject(new WebSearchPlugin(apiKey, engineId));

            var ttsService = new TextToSpeechService();
            var (history, isNew) = FileService.LoadConversation();

            if (isNew)
            {
                #region Propmt to Jarvis 
                history.AddSystemMessage(@"You are JARVIS - Just A Rather Very Intelligent System.
                Act as an advanced AI assistant with sophisticated, professional personality.

                PERSONALITY:
                - Intelligent, analytical, and proactive
                - Confident and precise in communication  
                - Professional tone with subtle wit
                - Address the user respectfully but naturally

                RESPONSE STYLE:
                - Concise but thorough in explanations
                - Natural, flowing language - not robotic
                - Add brief analytical insights when appropriate
                - Break down complex topics clearly

                CRITICAL FUNCTIONAL RULES:
                - For weather queries, ALWAYS call the actual WeatherRealTimePlugin
                - For time queries, always call the actual TimePlugin  
                - NEVER use cached responses from conversation history
                - ALWAYS fetch fresh data from the API
                - For unknown/time-sensitive info, use web search via [[SEARCH: your query here]]

                Maintain this personality while following all functional rules above.");
                #endregion
                Console.WriteLine("Started new Conversation");
            }
            else
            {
                Console.WriteLine($"Loaded last Conversation with {history.Count} messages");
            }

            var execSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };


            #region Voice input/output Always-listening voice with barge-in
            // Always-listening voice with barge-in

            using var voiceService = new NAudioVoiceService(
             onFinalText: async text =>
             {
                 #region Check Password 
                 // Lock phase: check spoken password first
                 if (System.Threading.Volatile.Read(ref _accessGranted) == 0)
                 {
                     var spoken = Normalize(text);
                     if (IsPasswordMatch(spoken, _expectedPwd!))
                     {
                         System.Threading.Volatile.Write(ref _accessGranted, 1);
                         ttsService.Speak("Access granted. Welcome! I'm Jarvis, built by Pawan. How can I assist you today?");

                     }
                     else
                     {
                         ttsService.Speak("Access denied. Please provide the password.");
                         _pwPromptSpoken = false;

                     }
                     return; // do not route to LLM while locked
                 }
                 #endregion
                 if (ttsService.IsSpeaking) return;

                 await ProcessMessageService.ProcessMessageAsync(text, history, kernel, execSettings, webSearch, ttsService);

             }, ttsService);


            #endregion

            Console.WriteLine(" Always-listening enabled (barge-in active). Speak naturally.");
            Console.WriteLine("Type 'q' to quit, 'voice' to toggle voice responses");

            voiceService.Start();

            try
            {
                await ChatLoopService.ChatLoop(history, kernel, execSettings, webSearch, ttsService);
            }
            finally
            {
                voiceService.Stop();
                FileService.SaveConversation(history);
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
