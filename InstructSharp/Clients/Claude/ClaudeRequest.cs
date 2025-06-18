using InstructSharp.Core;
using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Claude;
public class ClaudeRequest : ILLMRequest
{
    public string Model { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1000;
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;

}
