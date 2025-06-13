using InstructSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.LLama;
public class LLamaRequest : ILLMRequest
{
    public string Model { get; set; } = LLamaModels.Llama4Maverick17B;
    public string Instructions { get; set; }
    public string Input { get; set; }
    public double Temperature { get; set; } = 0.7;

}
