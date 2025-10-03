using System.Collections.Generic;
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
    public bool EnableWebSearch { get; set; }
    public bool EnableFileSearch { get; set; }
    public bool EnableImageGeneration { get; set; }
    public bool EnableCodeInterpreter { get; set; }
    public bool EnableComputerUse { get; set; }
    public List<ChatGPTToolSpecification> CustomTools { get; set; } = new();
}

public class ChatGPTToolSpecification
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
