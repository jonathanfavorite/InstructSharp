namespace InstructSharp.Interfaces;
public interface ILLMRequest
{
    string Model { get; set; }
    string Instructions { get; set; }
    string Input { get; set; }
    double Temperature { get; set; }
}
