using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Claude;
public class ClaudeRequest
{
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.7;
    public string Instructions { get; set; }
    public string Input { get; set; }
    public int MaxTokens { get; set; } = 1000;

}
