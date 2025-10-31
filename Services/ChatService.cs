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

    #region Start Chat Method

    private static int emptyInputCount;

    public static async Task StartChat(Kernel kernel)
    {
        var (history, isNewConversation) = FileService.LoadConversation();

        Console.WriteLine("Hey, I am your Personal Agent");

        if (isNewConversation)
        {
            history.AddSystemMessage(@"You are a helpful AI personal assistant. 
            Keep responses clear, concise, and friendly.
            Answer questions directly without unnecessary details.
            Use simple language that's easy to understand.

            CRITICAL: For weather queries, ALWAYS call the actual WeatherRealTimePlugin,
            for time queries , always call the actual TimePlugin.
            NEVER use cached responses from conversation history.
            ALWAYS fetch fresh data from the API.
            ");

            Console.WriteLine();
            Console.WriteLine("Started new Conversation");
        }
        else
        {
            Console.WriteLine($"Loaded last Conversation with {history.Count} messages");
        }

        // Auto function calling 
        OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        await ChatLoop(history, kernel, openAiPromptExecutionSettings);
        FileService.SaveConversation(history);
    }

    private static async Task ChatLoop(ChatHistory history, Kernel kernel,
      OpenAIPromptExecutionSettings executionSettings)
    {
        emptyInputCount = 0;
        while (true)
            try
            {
                Console.WriteLine();
                Console.Write("User > ");
                var userMessage = Console.ReadLine()!;

                #region Input Validation

                if (emptyInputCount >= 3)
                {
                    Console.WriteLine("Invalid Input, exiting...");
                    break;
                }

                if (string.IsNullOrWhiteSpace(userMessage))
                {
                    Console.WriteLine("Please enter a valid message");
                    emptyInputCount++;
                    continue;
                }

                if (userMessage.ToLower() == "exit" || userMessage.ToLower() == "quit")
                {
                    Console.WriteLine("Exiting...");
                    break;
                }

                #endregion

                // ---------- In ChatLoop (only the /pdf block) ----------
                if (userMessage.StartsWith("/pdf ", StringComparison.OrdinalIgnoreCase))
                {
                    var path = userMessage.Substring(5).Trim();
                    var pdfText = PdfService.LoadOrCreatePdf(path);

                    if (string.IsNullOrWhiteSpace(pdfText))
                    {
                        Console.WriteLine("PDF could not be loaded – skipping.");
                        continue;
                    }

                    history.AddUserMessage($"Here is the content of the PDF file:\n{pdfText}");
                    Console.WriteLine("PDF content loaded into chat context.");
                    continue;
                }

                // Add to history
                history.AddUserMessage(userMessage);

                var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
                var response = await chatCompletion.GetChatMessageContentAsync(
                    history,
                    executionSettings, kernel
                );

                #region Display response

                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("\nPersonal Assistant > ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(response);

                Console.ResetColor();

                #endregion

                // Add response to history FIRST
                history.AddAssistantMessage(response.Content);

                ManageConversation(history);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Sorry, something went wrong: {e.Message}");
            }
    }

    #endregion
}