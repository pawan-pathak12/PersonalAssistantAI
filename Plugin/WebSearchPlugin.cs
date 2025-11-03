using Microsoft.SemanticKernel;
using PersonalAssistantAI.Services;
using System.ComponentModel;

namespace PersonalAssistantAI.Plugin
{
    public class WebSearchPlugin
    {
        private readonly WebSearchService _webSearchService;
        public WebSearchPlugin(string apiKey, string engineId)
        {
            _webSearchService = new WebSearchService(apiKey, engineId);
        }

        [KernelFunction, Description("Searches the web for a given query to find recent or time-sensitive information.")]
        public async Task<string> Search(
       [Description("The topic or question to search for on the web.")] string query)
        {
            Console.WriteLine($"🔍 Performing web search for: {query}");
            return await _webSearchService.SearchAsync(query);
        }
    }
}
