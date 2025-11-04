using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class ProcessMessageService
    {
        public static async Task ProcessMessageAsync(
        string userMessage,
        ChatHistory history,
        Kernel kernel,
        OpenAIPromptExecutionSettings execSettings,
        WebSearchService webSearch,
        TextToSpeechService ttsService)
        {
            // Optional commands
            if (userMessage.StartsWith("/pdf ", StringComparison.OrdinalIgnoreCase))
            {
                var path = userMessage.Substring(5).Trim();
                var pdf = PdfService.LoadOrCreatePdf(path);
                if (string.IsNullOrWhiteSpace(pdf))
                {
                    Console.WriteLine("PDF could not be loaded.");
                    return;
                }
                history.AddUserMessage($"[PDF] {Path.GetFileName(path)}\n{pdf}");
                Console.WriteLine("PDF loaded into context.");
                return;
            }

            // Normal AI chat
            history.AddUserMessage(userMessage);

            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var answer = await chat.GetChatMessageContentAsync(history, execSettings, kernel);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write("\nPersonal Assistant > ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(answer);
            Console.ResetColor();

            history.AddAssistantMessage(answer.Content ?? string.Empty);

            var responseText = answer.Content ?? string.Empty;
            // Speak (will be interrupted on barge-in)
            if (!responseText.Contains("Listening...", StringComparison.OrdinalIgnoreCase) &&
                !responseText.StartsWith("How can I assist you", StringComparison.OrdinalIgnoreCase))
            {
                ttsService.Speak(responseText);
            }

            _ = Task.Run(() => ManageConversationService.ManageConversation(history));
            await Task.Delay(50);
        }
    }
}
