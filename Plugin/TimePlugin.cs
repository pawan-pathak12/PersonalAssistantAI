using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

public class TimePlugin
{
    private readonly string _apiKey;

    public TimePlugin()
    {
        _apiKey = SetupTimeApiKey();
    }

    private static string SetupTimeApiKey()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            var apiKey = configuration["AbstractTimeZoneAPI:ApiKey"];

            if (string.IsNullOrEmpty(apiKey)) throw new Exception("TimeZoneDB API key not found in appsettings.json");

            return apiKey;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load TimeZoneDB API key: {ex.Message}");
        }
    }

    [KernelFunction]
    [Description("Get the current time based on location")]
    public async Task<string> GetTimeAsync([Description("This is the location of which time to get")] string location)
    {
        if (string.IsNullOrEmpty(location)) return "Please provide a valid location name.";

        try
        {
            using var client = new HttpClient();
            var url =
                $"https://timezone.abstractapi.com/v1/current_time/?api_key={_apiKey}&location={Uri.EscapeDataString(location)}";

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return FormatTimeResponse(json, location);
        }
        catch
        {
            return "Error: Network error occurred while fetching time data.";
        }
    }


    private static string FormatTimeResponse(string json, string location)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("datetime", out var datetimeElement))
            {
                var formattedTime = datetimeElement.GetString();
                if (DateTime.TryParse(formattedTime, out var dateTime))
                    return $"Current time in {location} is {dateTime:hh:mm tt} on {dateTime:MMMM dd, yyyy}";
            }

            return $"Sorry, I couldn't get time for {location}.";
        }
        catch
        {
            return $"Sorry, I couldn't process time for {location}.";
        }
    }
}