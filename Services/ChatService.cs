using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
    private static int emptyInputCount;

    public static async Task StartChat(Kernel kernel)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            @"Yoy are helpful Personal Assistant build to 
                    response all user question in simple way");

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        //auto function calling 
        OpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };
        await ChatLoop(chatCompletionService, chatHistory, openAiPromptExecutionSettings, kernel);
    }

    private static async Task ChatLoop(IChatCompletionService chatCompletionService, ChatHistory history,
        OpenAIPromptExecutionSettings executionSettings, Kernel kernel)
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

                //get response from AI
                var result =
                    chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel);
                // Display response
                await DisplayResponse(history, result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
    }

    private static async Task DisplayResponse(ChatHistory chatHistory,
        IAsyncEnumerable<StreamingChatMessageContent> result)
    {
        //stream the result
        var fullmessageBUilder = new StringBuilder();
        var first = true;
        await foreach (var context in result)
        {
            if (context.Role.HasValue && first)
            {
                Console.Write("Personal Assistant >");
                first = false;
            }

            Console.Write(context.Content);
            fullmessageBUilder.Append(context.Content);
        }

        Console.WriteLine();
        var fullMessage = fullmessageBUilder.ToString();
        //Add the message to the chat history
        chatHistory.AddAssistantMessage(fullMessage);
    }
}