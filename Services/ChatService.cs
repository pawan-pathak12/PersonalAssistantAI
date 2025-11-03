using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalAssistantAI.Plugin;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
    private static int _accessGranted = 0;
    private static string? _expectedPwd;
    private static bool _pwPromptSpoken;
    #region New code 
    public static void ManageConversation(ChatHistory chatHistory)
    {
        if (chatHistory.Count > 100)
        {
            Console.WriteLine($"📝 Conversation getting long ({chatHistory.Count} messages).");
            Console.Write("How many old messages to remove? (0 to keep all): ");

            if (int.TryParse(Console.ReadLine(), out var messagesToRemove) && messagesToRemove > 0)
            {
                var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
                var recentMessages = chatHistory.TakeLast(chatHistory.Count - messagesToRemove).ToList();

                chatHistory.Clear();
                if (systemMessage != null) chatHistory.Add(systemMessage);
                foreach (var message in recentMessages) chatHistory.Add(message);
                Console.WriteLine($"Removed {messagesToRemove} old messages. Now {chatHistory.Count} messages");
            }
            else
            {
                Console.WriteLine("Keeping all messages");
            }
        }
    }

    public static async Task StartChat(Kernel kernel)
    {
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

        #region Voice input/output
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
 });
        #endregion

        Console.WriteLine("🎤 Always-listening enabled (barge-in active). Speak naturally.");
        Console.WriteLine("Type 'q' to quit, 'voice' to toggle voice responses");

        voiceService.Start();

        try
        {
            await ChatLoop(history, kernel, execSettings, webSearch, ttsService);
        }
        finally
        {
            voiceService.Stop();
            FileService.SaveConversation(history);
        }
    }


    private static async Task ChatLoop(
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

                await ProcessMessageAsync(input, history, kernel, exec, webSearch, ttsService);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in chat loop: {ex.Message}");
            }
        }
    }

    private static async Task ProcessMessageAsync(
        string userMessage,
        ChatHistory history,
        Kernel kernel,
        OpenAIPromptExecutionSettings execSettings,
        WebSearchService webSearch,
        TextToSpeechService ttsService)
    {
        // Optional commands
        if (userMessage.StartsWith("/pdf ", StringComparison.OrdinalIgnoreCase))
        {
            var path = userMessage.Substring(5).Trim();
            var pdf = PdfService.LoadOrCreatePdf(path);
            if (string.IsNullOrWhiteSpace(pdf))
            {
                Console.WriteLine("PDF could not be loaded.");
                return;
            }
            history.AddUserMessage($"[PDF] {Path.GetFileName(path)}\n{pdf}");
            Console.WriteLine("PDF loaded into context.");
            return;
        }

        // Normal AI chat
        history.AddUserMessage(userMessage);

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var answer = await chat.GetChatMessageContentAsync(history, execSettings, kernel);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("\nPersonal Assistant > ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(answer);
        Console.ResetColor();

        history.AddAssistantMessage(answer.Content ?? string.Empty);

        var responseText = answer.Content ?? string.Empty;
        // Speak (will be interrupted on barge-in)
        if (!responseText.Contains("Listening...", StringComparison.OrdinalIgnoreCase) &&
            !responseText.StartsWith("How can I assist you", StringComparison.OrdinalIgnoreCase))
        {
            ttsService.Speak(responseText);
        }

        _ = Task.Run(() => ManageConversation(history));
        await Task.Delay(50);
    }

    #region Helper method for Passwod Check 
    private static string Normalize(string s)
=> new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    private static bool IsPasswordMatch(string candidate, string expected)
    => candidate == expected;
    #endregion
}

#endregion
