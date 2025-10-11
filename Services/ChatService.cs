using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using PersonalAssistantAI.Agents;

namespace PersonalAssistantAI.Services;

public static class ChatService
{
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

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

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

                // Display response
                Console.WriteLine($"Personal Assistant > {response}");
                history.AddAssistantMessage(response);
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