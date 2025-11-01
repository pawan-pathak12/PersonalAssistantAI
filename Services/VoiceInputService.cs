// Services/VoiceInputService.cs
using System.Speech.Recognition;

namespace PersonalAssistantAI.Services;

public class VoiceInputService
{
    private readonly SpeechRecognitionEngine _recognizer;
    private readonly Action<string> _onRecognized;
    public bool IsListening { get; private set; }

    public VoiceInputService(Action<string> onRecognized)
    {
        _onRecognized = onRecognized;
        _recognizer = new SpeechRecognitionEngine();
        _recognizer.LoadGrammar(new DictationGrammar());
        _recognizer.SetInputToDefaultAudioDevice();
        _recognizer.SpeechRecognized += OnSpeechRecognized;
        _recognizer.SpeechHypothesized += (s, e) => Console.Write($"\rHearing: {e.Result.Text}… ");
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence > 0.7)
        {
            Console.WriteLine($"\rYou said: {e.Result.Text}");
            _onRecognized(e.Result.Text);
        }
    }

    public void Start()
    {
        _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        IsListening = true;
    }

    public void Stop()
    {
        _recognizer.RecognizeAsyncStop();
        IsListening = false;
    }
}