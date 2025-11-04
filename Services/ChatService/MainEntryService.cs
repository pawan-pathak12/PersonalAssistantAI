using Microsoft.SemanticKernel;
using PersonalAssistantAI.Plugin;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class MainEntryService
    {
        public static async Task StartJarvis()
        {
            //step 1 : Create Kernel 
            var kernel = CreateBuilder();

            //step 2 : set plugin 
            kernel.Plugins.AddFromType<TimePlugin>();
            kernel.Plugins.AddFromType<WeatherPlugin>();
            kernel.Plugins.AddFromType<PdfPlugin>();


            //step 3 :Start Chat System 
            //call ChatOrchestrator 
            ChatOrchestrator.Start(kernel);
        }

        static Kernel CreateBuilder()
        {
            return Kernel.CreateBuilder().
                  AddOpenAIChatCompletion("qwen2.5:7b",
                  "not-needed",
                  httpClient: new HttpClient
                  {
                      BaseAddress = new Uri("http://localhost:11434/v1")
                  })
              .Build();
        }
    }
}
