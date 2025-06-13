namespace InstructSharp.Clients.Grok;
public class GrokRequest
{
    public string Model { get; set; } = GrokModels.Grok3;
    public string Instructions { get; set; }
    public string Input { get; set; }
}
