using Microsoft.SemanticKernel;

namespace PersonalAssistantAI.Services.ChatService
{
    internal class ChatOrchestrator
    {


        // this class will coneect all the chatservice class and use in MainEntryPoint class and then it will be call in program.cs


        public static async Task Start(Kernel kernel)
        {
            StartChatService.StartChat(kernel);

        }
    }
}
