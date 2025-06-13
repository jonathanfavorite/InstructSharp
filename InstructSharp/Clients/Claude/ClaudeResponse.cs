using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Claude;
internal class ClaudeResponse
{
    public string id { get; set; }
    public string type { get; set; }
    public string role { get; set; }
    public string model { get; set; }
    public List<ClaudeContent> content { get; set; }
    public string stop_reason { get; set; }
    public object stop_sequence { get; set; }
    public ClaudeUsage usage { get; set; }
}

internal class ClaudeContent
{
    public string type { get; set; }
    public string text { get; set; }

    // These three appear only when type=="tool_use"
    public string id { get; set; }
    public string name { get; set; }
    public JsonElement input { get; set; }    // holds your { Question, Answer } object
}



internal class ClaudeUsage
{
    public int input_tokens { get; set; }
    public int cache_creation_input_tokens { get; set; }
    public int cache_read_input_tokens { get; set; }
    public int output_tokens { get; set; }
    public string service_tier { get; set; }
}

