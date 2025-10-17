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
        You are a helpful Personal Assistant with access to tools.

        AVAILABLE TOOLS:
        - TaskPlugin: AddTask, GetTasks, CompleteTask (for task management)
        - CalculatorPlugin: Add, Subtract, Multiply, Divide (for calculations)

        CRITICAL RULES:
        1. You MUST ACTUALLY EXECUTE tools - NEVER simulate or pretend to use them
        2. When user asks for calculations, ALWAYS call CalculatorPlugin
        3. When user mentions tasks, ALWAYS call TaskPlugin  
        4. Show tool calls but not  code to the user
        5. Only show final results in natural language

        FAILURE to actually call tools will result in incorrect responses.
        ");

    }

    public async Task<string> ProcessAsync(string userMessage)
    {
        //Detect commands here 
        _chatHistory.AddUserMessage(userMessage);
        var response = await _ChatCompletionService.GetChatMessageContentAsync(_chatHistory);
        _chatHistory.AddAssistantMessage(response.Content);
        return response.Content ?? "No Response from Personal Agent";
    }
}