using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalAssistantAI.Agents;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
    /*   CORE FEATURES:
           //todo : implement Real Time System to give : Weather Plugin
           //todo : implement Real Time System to give : Time Plugin
           //todo : implement Real Time System to give : News Plugin (RSS feeds)
           //todo : implement Real Time System to give : Currency Converter Plugin
           //todo : implement Real Time System to give : Unit Converter Plugin
    */

    #region  Start Chat Method

    private static int emptyInputCount;

    public static async Task StartChat(Kernel kernel)
    {
        var (history, isNewConversation) = FileService.LoadConversation();

        var personalAgent = new PersonalAssistantAgent(kernel);
        Console.WriteLine("Hey , I am your Personal Agent ");
        if (isNewConversation)
            history.AddSystemMessage(
                @"Yoy are helpful Personal Assistant build to 
                    response all user question in simple way");

        kernel.GetRequiredService<IChatCompletionService>();

        //auto function calling 
        OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        await ChatLoop(history, personalAgent);
        FileService.SaveConversation(history);
    }

    private static async Task ChatLoop(ChatHistory history, PersonalAssistantAgent agent)
    {
        while (true)
            try
            {
                Console.WriteLine();
                Console.Write("User >");
                var userMessage = Console.ReadLine()!;

                #region Input Validation

                if (emptyInputCount >= 3)
                {
                    Console.WriteLine("Invalid Input , exiting......");
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
                    Console.WriteLine("exting ....");
                    break;
                }

                #endregion

                //Add to history 
                history.AddUserMessage(userMessage);

                var response = await agent.ProcessAsync(userMessage);
                //get response from AI
                Console.ForegroundColor = ConsoleColor.Blue;
                // Display response
                Console.Write($"Personal Assistant > ");
                ManageConversation(history);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(response);

                history.AddAssistantMessage(response);
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.WriteLine("Sorry, something went wrong. Please try again.");
                throw;
            }
    }

    #endregion

    #region Conversation Monitor
    public static void ManageConversation(ChatHistory chatHistory)
    {
        if (chatHistory.Count > 20)
        {

            Console.WriteLine($"📝 Conversation getting long ({chatHistory.Count} messages).");
            Console.Write("How many old messages to remove? (0 to keep all): ");

            if (int.TryParse(Console.ReadLine(), out int messagesToRemove) && messagesToRemove > 0)
            {
                var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
                var recentMessages = chatHistory.TakeLast(chatHistory.Count - messagesToRemove).ToList();

                chatHistory.Clear();
                if (systemMessage != null)
                {
                    chatHistory.Add(systemMessage);
                }
                foreach (var message in recentMessages)
                {
                    chatHistory.Add(message);

                }
                Console.WriteLine($"Removed {messagesToRemove} old Messages.Now {chatHistory.Count} messages ");
            }
            else
            {
                Console.WriteLine("Keeping all messages");
            }

        }
    }


    #endregion


}