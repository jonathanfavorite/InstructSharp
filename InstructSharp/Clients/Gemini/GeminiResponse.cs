using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Gemini;

internal class GeminiResponse
{
    public List<GeminiCandidate> candidates { get; set; }
    public GeminiUsageMetadata usageMetadata { get; set; }
    public string modelVersion { get; set; }
    public string responseId { get; set; }
}


internal class GeminiCandidate
{
    public GeminiContent content { get; set; }
    public string finishReason { get; set; }
    public int index { get; set; }
}

internal class GeminiContent
{
    public List<GeminiPart> parts { get; set; }
    public string role { get; set; }
}

internal class GeminiPart
{
    public string text { get; set; }
}

internal class GeminiPromptTokensDetail
{
    public string modality { get; set; }
    public int tokenCount { get; set; }
}

internal class GeminiUsageMetadata
{
    public int promptTokenCount { get; set; }
    public int candidatesTokenCount { get; set; }
    public int totalTokenCount { get; set; }
    public List<GeminiPromptTokensDetail> promptTokensDetails { get; set; }
    public int thoughtsTokenCount { get; set; }
}


