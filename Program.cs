using Microsoft.SemanticKernel;
using PersonalAssistantAI.Plugin;
using PersonalAssistantAI.Services;

// Setup Kernel
var kernel = CreateKernel();
CreateKernel();

//plugin 
kernel.Plugins.AddFromType<WeatherPlugin>();


await ChatService.StartChat(kernel);

#region Connection to Ollama

static Kernel CreateKernel()
{
    return Kernel.CreateBuilder()
        .AddOpenAIChatCompletion(
            "qwen2.5:7b",
            "not-needed",
            httpClient: new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434/v1")
            })
        .Build();
}


#endregion

