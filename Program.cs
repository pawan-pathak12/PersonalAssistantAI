using Microsoft.SemanticKernel;
using PersonalAssistantAI.Plugin;
using PersonalAssistantAI.Services;
using System.Speech.Synthesis;



// Setup Kernel
var kernel = CreateKernel();
CreateKernel();

//plugin 
kernel.Plugins.AddFromType<WeatherPlugin>();
kernel.Plugins.AddFromType<TimePlugin>();
kernel.Plugins.AddFromType<PdfPlugin>();

await ChatService.StartChat(kernel);
//TestTTS();

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

# region ai voice test 
static void TestTTS()
{
    try
    {
        var synthesizer = new SpeechSynthesizer();
        Console.WriteLine("Available Voices:");
        foreach (var voice in synthesizer.GetInstalledVoices())
        {
            Console.WriteLine($"- {voice.VoiceInfo.Name}");
        }

        Console.WriteLine("Testing TTS...");
        synthesizer.Speak("JARVIS is now active");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"TTS Error: {ex.Message}");
    }
}
#endregion