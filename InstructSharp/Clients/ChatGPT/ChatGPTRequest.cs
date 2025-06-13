using InstructSharp.Interfaces;

namespace InstructSharp.Clients.ChatGPT;
public class ChatGPTRequest : ILLMRequest
{
    public string Model { get; set; } = "gpt-4o-mini";
    public string Instructions { get; set; }
    public string Input { get; set; }
    public double Temperature { get; set; } = 0.7;
}
