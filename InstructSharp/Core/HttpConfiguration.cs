namespace InstructSharp.Core;
public class HttpConfiguration
{
    public string BaseUrl { get; set; }
    public Dictionary<string, string> DefaultHeaders { get; set; } = new Dictionary<string, string>();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public string ApiKey { get; set; }
    public string Model { get; set; } // used by Gemini
    public bool SetupBaseUrlAfterConstructor { get; set; } = false; // used by Gemini to set the base URL after the constructor
}
