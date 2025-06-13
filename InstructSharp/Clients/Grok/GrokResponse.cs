using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Grok;

internal class GrokResponse
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<GrokChoice> choices { get; set; }
    public GrokUsage usage { get; set; }
    public string system_fingerprint { get; set; }
}

internal class GrokChoice
{
    public int index { get; set; }
    public GrokMessage message { get; set; }
    public string finish_reason { get; set; }
}

internal class GrokCompletionTokensDetails
{
    public int reasoning_tokens { get; set; }
    public int audio_tokens { get; set; }
    public int accepted_prediction_tokens { get; set; }
    public int rejected_prediction_tokens { get; set; }
}

internal class GrokMessage
{
    public string role { get; set; }
    public string content { get; set; }
    public object refusal { get; set; }
}

internal class GrokPromptTokensDetails
{
    public int text_tokens { get; set; }
    public int audio_tokens { get; set; }
    public int image_tokens { get; set; }
    public int cached_tokens { get; set; }
}

internal class GrokUsage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
    public GrokPromptTokensDetails prompt_tokens_details { get; set; }
    public GrokCompletionTokensDetails completion_tokens_details { get; set; }
    public int num_sources_used { get; set; }
}

