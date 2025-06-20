using InstructSharp.Core;
using InstructSharp.Interfaces;

namespace InstructSharp.Clients.Grok;
public class GrokRequest : ILLMRequest
{
    public string Model { get; set; } = GrokModels.Grok3;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
}
