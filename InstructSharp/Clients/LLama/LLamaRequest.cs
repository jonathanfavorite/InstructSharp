﻿using InstructSharp.Core;
using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.LLama;
public class LLamaRequest : ILLMRequest
{
    public string Model { get; set; } = LlamaModels.Llama4_Maverick_17B_128E;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public double Temperature { get; set; } = 0.7;
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
    public bool Stream { get; set; }

}
