using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
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
        while (true)
        {
            Console.WriteLine();
            Console.Write("User >");
            var userMessage = Console.ReadLine()!;

            if (userMessage.ToLower() == "exit" || userMessage.ToLower() == "quit")
            {
                Console.WriteLine("exiting...");
                break;
            }

            chatHistory.AddUserMessage(userMessage);
            //get response from AI
            var result = chatCompletionService.GetStreamingChatMessageContentsAsync(chatHistory,
                openAiPromptExecutionSettings,
                kernel);

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
    
}