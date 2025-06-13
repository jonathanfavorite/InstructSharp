using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.DeepSeek;
public class DeepSeekRequest
{
    public string Model { get; set; } = DeepSeekModels.DeepSeekChat;
    public string Instructions { get; set; }
    public string Input { get; set; }
    public double Temperature { get; set; } = 0.7;
}
