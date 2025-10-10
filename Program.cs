using Microsoft.SemanticKernel;
using PersonalAssistantAI.Services;

namespace PersonalAssistantAI;

public class Program
{
    public static async Task Main()
    {
        var builder = Kernel.CreateBuilder();

        #region Connection to Ollama

        builder.AddOpenAIChatCompletion(
            "qwen2.5:7b",
            "not-needed",
            httpClient: new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434/v1")
            });
        var kernel = builder.Build();
        await ChatService.StartChat(kernel);

        #endregion
    }
}