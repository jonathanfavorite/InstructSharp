namespace InstructSharp.Core;
public class HttpConfiguration
{
    public string BaseUrl { get; set; }
    public string UrlPattern { get; set; } // New property for URL patterns like "https://api.example.com/{model}/endpoint (like Gemini)
    public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string ApiKey { get; set; }
    public string Model { get; set; }
    public bool SetupBaseUrlAfterConstructor { get; set; } = false;
}
