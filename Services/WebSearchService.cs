using System.Text.Json;

namespace PersonalAssistantAI.Services
{
    public class WebSearchService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _cx;

        public WebSearchService(string apiKey, string searchEngineId)
        {
            _http = new HttpClient();
            _apiKey = apiKey;
            _cx = searchEngineId;
        }

        public async Task<string> SearchAsync(string query)
        {
            var url = $"https://www.googleapis.com/customsearch/v1" +
                      $"?key={_apiKey}" +
                      $"&cx={_cx}" +
                      $"&q={Uri.EscapeDataString(query)}" +
                      $"&num=3";

            try
            {
                var json = await _http.GetStringAsync(url);
                var doc = JsonDocument.Parse(json);

                var sb = new System.Text.StringBuilder();
                foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                {
                    var title = item.GetProperty("title").GetString() ?? "";
                    var snippet = item.GetProperty("snippet").GetString() ?? "";
                    var link = item.GetProperty("link").GetString() ?? "";

                    sb.AppendLine($"Title: {title}");
                    sb.AppendLine($"Snippet: {snippet}");
                    sb.AppendLine($"Link: {link}");
                    sb.AppendLine("---");
                }

                return sb.Length > 0 ? sb.ToString() : "No results found.";
            }
            catch (Exception ex)
            {
                return $"Search error: {ex.Message}";
            }
        }
    }
}
