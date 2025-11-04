using PersonalAssistantAI.Services.ChatService;
using System.Speech.Synthesis;


await MainEntryService.StartJarvis();

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
static void ListAllVoices()
{
    try
    {
        using var synthesizer = new SpeechSynthesizer();
        Console.WriteLine("🎙️ Available Voices:");
        Console.WriteLine("====================");

        foreach (var voice in synthesizer.GetInstalledVoices())
        {
            var info = voice.VoiceInfo;
            Console.WriteLine($"🔊 {info.Name}");
            Console.WriteLine($"   Culture: {info.Culture}");
            Console.WriteLine($"   Gender: {info.Gender}");
            Console.WriteLine($"   Age: {info.Age}");
            Console.WriteLine($"   Description: {info.Description}");
            Console.WriteLine();
        }

        // Test each voice quickly
        Console.WriteLine("Testing each voice (2 seconds each)...");
        foreach (var voice in synthesizer.GetInstalledVoices())
        {
            var info = voice.VoiceInfo;
            Console.Write($"Testing {info.Name}... ");
            synthesizer.SelectVoice(info.Name);
            synthesizer.SpeakAsync("Hello, I am JARVIS");
            Thread.Sleep(2000);
            synthesizer.SpeakAsyncCancelAll();
            Console.WriteLine("✓");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
#endregion