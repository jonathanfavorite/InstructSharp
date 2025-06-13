using InstructSharp.Interfaces;

namespace InstructSharp.Clients.Grok;
public class GrokRequest : ILLMRequest
{
    public string Model { get; set; } = GrokModels.Grok3;
    public string Instructions { get; set; }
    public string Input { get; set; }
    public double Temperature { get; set; }
}
