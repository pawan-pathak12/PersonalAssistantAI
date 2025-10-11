using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PersonalAssistantAI.Agents;

public class PersonalAssistantAgent
{
    private readonly IChatCompletionService _ChatCompletionService;
    private readonly ChatHistory _chatHistory;

    public PersonalAssistantAgent(Kernel kernel)
    {
        _ChatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(@"
        You are a helpful Personal Assistant that helps with:
        - Task and reminder management
        - Note taking and organization  
        - Simple calculations and information
        - General questions and assistance

        Keep responses clear, concise, and actionable.
        Use natural, friendly conversation.");
    }

    public async Task<string> ProcessAsync(string userMessage)
    {
        //Detect commands here 
        if (userMessage.ToLower().Contains("remind"))
        {
        }

        if (userMessage.ToLower().Contains("note"))
        {
        }

        if (userMessage.ToLower().Contains("remember"))
        {
        }

        if (userMessage.ToLower().Contains("what time"))
        {
        }

        if (userMessage.ToLower().Contains("Calculate"))
        {
        }

        _chatHistory.AddUserMessage(userMessage);
        var response = await _ChatCompletionService.GetChatMessageContentAsync(_chatHistory);
        _chatHistory.AddAssistantMessage(response.Content);
        return response.Content ?? "No Response from Personal Agent";
    }
}