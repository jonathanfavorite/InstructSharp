using InstructSharp.Core;

namespace InstructSharp.Interfaces;
public interface ILLMRequest
{
    string Model { get; set; }
    string Instructions { get; set; }
    string Input { get; set; }
    double Temperature { get; set; }
    List<LLMImageRequest> Images { get; set; }
    bool ContainsImages => Images.Count > 0;
}
