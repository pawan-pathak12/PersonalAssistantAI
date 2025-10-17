using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace PersonalAssistantAI.Services;

public static class FileService
{
    private static readonly string HistoryFile = "chat_history.json";

    public static void SaveConversation(ChatHistory history)
    {
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(HistoryFile, json);
        Console.WriteLine($" Conversation saved ({history.Count} messages)");
    }

    public static (ChatHistory history, bool isNew) LoadConversation()
    {
        if (!File.Exists(HistoryFile))
            return (new ChatHistory(), true); // New conversation

        var json = File.ReadAllText(HistoryFile);
        var history = JsonSerializer.Deserialize<ChatHistory>(json) ?? new ChatHistory();
        return (history, false); // Existing conversation
    }

    public static void SaveToFile<T>(string filePath, T data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public static T LoadFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
            return default;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }
}