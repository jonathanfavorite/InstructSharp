namespace InstructSharp.Clients.ChatGPT;
public class ChatGPTRequest
{
    public string Model { get; set; } = "gpt-4o-mini";
    public string? Instruction { get; set; }
    public string? Input { get; set; }
    public double? Temperature { get; set; } = 0.7;
}
