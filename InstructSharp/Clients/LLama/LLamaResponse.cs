using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.LLama;

internal class LLamaResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }
}

internal class Choice
{
    public int index { get; set; }
    public Message message { get; set; }
    public string finish_reason { get; set; }
    public object logprobs { get; set; }
}

internal class Message
{
    public string role { get; set; }
    public string content { get; set; }
    public object name { get; set; }
    public List<object> tool_calls { get; set; }
}

internal class Usage
{
    public int prompt_tokens { get; set; }
    public int total_tokens { get; set; }
    public int completion_tokens { get; set; }
    public double estimated_cost { get; set; }
}

