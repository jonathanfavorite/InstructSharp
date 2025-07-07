using InstructSharp.Core;
using InstructSharp.Interfaces;

namespace InstructSharp.Clients.ChatGPT;
public class ChatGPTRequest : ILLMRequest
{
    public string Model { get; set; } = ChatGPTModels.GPT4o;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
    public bool Stream { get; set; }
}
