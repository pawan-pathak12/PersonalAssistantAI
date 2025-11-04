using Microsoft.SemanticKernel.ChatCompletion;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class ManageConversationService
    {

        public static void ManageConversation(ChatHistory chatHistory)
        {
            if (chatHistory.Count > 100)
            {
                Console.WriteLine($"📝 Conversation getting long ({chatHistory.Count} messages).");
                Console.Write("How many old messages to remove? (0 to keep all): ");

                if (int.TryParse(Console.ReadLine(), out var messagesToRemove) && messagesToRemove > 0)
                {
                    var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
                    var recentMessages = chatHistory.TakeLast(chatHistory.Count - messagesToRemove).ToList();

                    chatHistory.Clear();
                    if (systemMessage != null) chatHistory.Add(systemMessage);
                    foreach (var message in recentMessages) chatHistory.Add(message);
                    Console.WriteLine($"Removed {messagesToRemove} old messages. Now {chatHistory.Count} messages");
                }
                else
                {
                    Console.WriteLine("Keeping all messages");
                }
            }
        }
    }
}
