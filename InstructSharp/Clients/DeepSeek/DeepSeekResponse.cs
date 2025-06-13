using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.DeepSeek;
internal class DeepSeekChoice
{
    public int index { get; set; }
    public DeepSeekMessage message { get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}

internal class DeepSeekMessage
{
    public string role { get; set; }
    public string content { get; set; }
}

internal class DeepSeekPromptTokensDetails
{
    public int cached_tokens { get; set; }
}

internal class DeepSeekResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<DeepSeekChoice> choices { get; set; }
    public DeepSeekUsage usage { get; set; }
    public string system_fingerprint { get; set; }
}

internal class DeepSeekUsage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
    public DeepSeekPromptTokensDetails prompt_tokens_details { get; set; }
    public int prompt_cache_hit_tokens { get; set; }
    public int prompt_cache_miss_tokens { get; set; }
}


