using InstructSharp.Core;
using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.DeepSeek;
public class DeepSeekRequest : ILLMRequest
{
    public string Model { get; set; } = DeepSeekModels.DeepSeekChat;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
    public bool Stream { get; set; }
}
