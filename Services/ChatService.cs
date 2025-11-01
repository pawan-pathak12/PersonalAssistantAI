using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
    #region Conversation Monitor

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

    #endregion

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
        #region Load configuration (Google Search)
        var config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();

        var google = config.GetSection("GoogleSearch");
        var apiKey = google["ApiKey"] ?? throw new InvalidOperationException("Google API key missing");
        var engineId = google["SearchEngineId"] ?? throw new InvalidOperationException("Search engine ID missing");
        #endregion

        var webSearch = new WebSearchService(apiKey, engineId);

        var (history, isNew) = FileService.LoadConversation();

        Console.WriteLine("Hey, I am your Personal Agent");

        if (isNew)
        {
            history.AddSystemMessage(@"You are a helpful AI personal assistant. 
            Keep responses clear, concise, and friendly.
            Answer questions directly without unnecessary details.
            Use simple language that's easy to understand.

            CRITICAL: For weather queries, ALWAYS call the actual WeatherRealTimePlugin,
            for time queries, always call the actual TimePlugin.
            NEVER use cached responses from conversation history.
            ALWAYS fetch fresh data from the API.

            If you don't know the answer or it's time-sensitive (news, facts, current events), 
            use the web search tool by responding with: [[SEARCH: your query here]]");
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

        // ---------- Voice ----------
        var cts = new CancellationTokenSource();
        var voice = new VoiceInputService(text =>
            _ = ProcessMessageAsync(text, history, kernel, execSettings, webSearch, cts.Token));

        voice.Start();
        Console.WriteLine("Voice input ON (speak, pause to send)");
        Console.WriteLine("Press V to toggle voice | Q to quit");

        // ---------- Keyboard loop ----------
        await ChatLoop(history, kernel, execSettings, webSearch, voice, cts.Token);

        // ---------- Clean-up ----------
        voice.Stop();
        FileService.SaveConversation(history);
    }

    private static async Task ChatLoop(
        ChatHistory history,
        Kernel kernel,
        OpenAIPromptExecutionSettings exec,
        WebSearchService webSearch,
        VoiceInputService voice,
        CancellationToken ct)
    {
        int empty = 0;

        while (!ct.IsCancellationRequested)
        {
            Console.WriteLine();
            Console.Write("User > ");
            var input = Console.ReadLine()!.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                if (++empty >= 3) { Console.WriteLine("Too many empty lines – exiting."); break; }
                Console.WriteLine("Please type or speak something.");
                continue;
            }

            empty = 0;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            // ----- toggle voice -----
            if (input.Equals("v", StringComparison.OrdinalIgnoreCase))
            {
                if (voice.IsListening) { voice.Stop(); Console.WriteLine("Voice OFF – press V to turn on"); }
                else { voice.Start(); Console.WriteLine("Voice ON"); }
                continue;
            }

            await ProcessMessageAsync(input, history, kernel, exec, webSearch, ct);
        }
    }

    // -------------------------------------------------
    // 3. SINGLE MESSAGE PROCESSOR – used by BOTH keyboard & voice
    // -------------------------------------------------
    private static async Task ProcessMessageAsync(
        string userMessage,
        ChatHistory history,
        Kernel kernel,
        OpenAIPromptExecutionSettings exec,
        WebSearchService webSearch,
        CancellationToken ct)
    {
        // ----- /pdf -----
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

        // ----- /search -----
        if (userMessage.StartsWith("/search ", StringComparison.OrdinalIgnoreCase))
        {
            var q = userMessage.Substring(8).Trim();
            if (string.IsNullOrWhiteSpace(q)) { Console.WriteLine("Add a query after /search"); return; }

            Console.WriteLine("Searching web…");
            var results = await webSearch.SearchAsync(q);
            if (results.StartsWith("Search error:")) { Console.WriteLine(results); return; }

            history.AddUserMessage($"Web results for \"{q}\":\n{results}");
            Console.WriteLine("Web results added to context.");
            return;
        }

        // ----- Normal AI chat -----
        history.AddUserMessage(userMessage);

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var answer = await chat.GetChatMessageContentAsync(history, exec, kernel);

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("\nPersonal Assistant > ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(answer);
        Console.ResetColor();

        history.AddAssistantMessage(answer.Content);
        ManageConversation(history);
    }
}
