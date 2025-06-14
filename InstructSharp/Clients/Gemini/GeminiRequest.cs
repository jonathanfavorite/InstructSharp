﻿using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Gemini;
public class GeminiRequest : ILLMRequest
{
    public string Model { get; set; }
    public string Instructions { get; set; }
    public string Input { get; set; }
    public double Temperature { get; set; }
}
