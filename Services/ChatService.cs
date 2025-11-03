using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalAssistantAI.Plugin;

namespace PersonalAssistantAI.Services;

public static class ChatService
{


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


    #region ToDo

    /*   CORE FEATURES:
           //todo :done : implement Real Time System to give : Weather Plugin
           //todo :done : implement Real Time System to give : Time Plugin
           //todo : implement Real Time System to give : News Plugin (RSS feeds)
           //todo : implement Real Time System to give : Currency Converter Plugin
           //todo : implement Real Time System to give : Unit Converter Plugin
    */

    #endregion

    public static async Task StartChat(Kernel kernel)
    {
        #region Load configuration
        var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

        var google = config.GetSection("GoogleSearch");
        var apiKey = google["ApiKey"] ?? throw new InvalidOperationException("Google API key missing");
        var engineId = google["SearchEngineId"] ?? throw new InvalidOperationException("Search engine ID missing");
        #endregion

        var webSearch = new WebSearchService(apiKey, engineId);
        kernel.Plugins.AddFromObject(new WebSearchPlugin(apiKey, engineId));

        var ttsService = new TextToSpeechService();
        var (history, isNew) = FileService.LoadConversation();

        #region JARVIS Prompt
        if (isNew)
        {
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
            Console.WriteLine("Started new Conversation");
        }
        else
        {
            Console.WriteLine($"Loaded last Conversation with {history.Count} messages");
        }
        #endregion

        var execSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        // Always-listening NAudio voice service
        using var voiceService = new NAudioVoiceService(async text =>
        {
            // Avoid feedback: ignore mic input while TTS is speaking
            if (ttsService.IsSpeaking) return;

            await ProcessMessageAsync(text, history, kernel, execSettings, webSearch, ttsService);
        });

        Console.WriteLine("🎤 Always-listening enabled (NAudio + VAD). Speak naturally.");
        Console.WriteLine("Type 'q' to quit, 'voice' to toggle voice responses");

        voiceService.Start(); // continuous listening

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
            try
            {
                if (!ttsService.IsSpeaking)
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

                    await ProcessMessageAsync(input, history, kernel, exec, webSearch, ttsService);
                }
                else
                {
                    await Task.Delay(100); // wait while TTS is talking
                }
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
        #region PDF Processing
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
        #endregion

        #region Web Search Command (optional)
        if (userMessage.StartsWith("/search ", StringComparison.OrdinalIgnoreCase))
        {
            var q = userMessage.Substring(8).Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                Console.WriteLine("Add a query after /search");
                return;
            }

            Console.WriteLine("Searching web…");
            var results = await webSearch.SearchAsync(q);
            if (results.StartsWith("Search error:", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(results);
                return;
            }

            history.AddUserMessage($"Web results for \"{q}\":\n{results}");
            Console.WriteLine("Web results added to context.");
            return;
        }
        #endregion

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
        if (!responseText.Contains("Listening...", StringComparison.OrdinalIgnoreCase) &&
            !responseText.StartsWith("How can I assist you", StringComparison.OrdinalIgnoreCase))
        {
            ttsService.Speak(responseText);
        }

        _ = Task.Run(() => ManageConversation(history));

        await Task.Delay(300); // small delay to let TTS start
    }
    #endregion
}