using InstructSharp.Core;
using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Gemini;
public class GeminiRequest : ILLMRequest
{
    public string Model { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
    public bool Stream { get; set; }
}
