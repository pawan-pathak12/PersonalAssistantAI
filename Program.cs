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

        #endregion

        Console.WriteLine("=================================");
        Console.WriteLine("🤖 PERSONAL ASSISTANT AI");
        Console.WriteLine("=================================");
        Console.WriteLine("1. Start Chat");
        Console.WriteLine("2. Exit");
        Console.Write("Choose option: ");

        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await ChatService.StartChat(kernel);
                break;
            case "2":
                Console.WriteLine("Goodbye! 👋");
                return;
            default:
                Console.WriteLine(" Invalid choice. Please enter 1 or 2.");
                break;
        }
    }
}