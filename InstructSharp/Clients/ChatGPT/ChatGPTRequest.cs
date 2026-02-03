using System.Collections.Generic;
using InstructSharp.Core;
using InstructSharp.Interfaces;

namespace InstructSharp.Clients.ChatGPT;
public class ChatGPTRequest : ILLMRequest
{
    public string Model { get; set; } = ChatGPTModels.GPT4o;
    public string Instructions { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? PreviousResponseId { get; set; }
    public double Temperature { get; set; } = 0.7;
    public double? TopP { get; set; }
    public int? MaxOutputTokens { get; set; }
    public int? MaxToolCalls { get; set; }
    public bool? ParallelToolCalls { get; set; }
    public bool? Store { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool? Background { get; set; }
    public string? PromptCacheKey { get; set; }
    public string? PromptCacheRetention { get; set; }
    public string? SafetyIdentifier { get; set; }
    public string? ServiceTier { get; set; }
    public string? TextVerbosity { get; set; }
    public List<LLMImageRequest> Images { get; set; } = new();
    public bool ContainsImages => Images.Count > 0;
    public bool Stream { get; set; }
    public bool EnableWebSearch { get; set; }
    public string WebSearchToolType { get; set; } = ChatGPTToolChoice.WebSearchPreview;
    public string WebSearchContextSize { get; set; } = "medium";
    public string? WebSearchUserCountry { get; set; } = "US";
    public bool EnableFileSearch { get; set; }
    public bool EnableImageGeneration { get; set; }
    public bool EnableCodeInterpreter { get; set; }
    public bool EnableComputerUse { get; set; }
    public List<ChatGPTToolSpecification> CustomTools { get; set; } = new();
    public ChatGPTToolChoice? ToolChoice { get; set; }
    public ChatGPTReasoningOptions? Reasoning { get; set; }
    public List<string> Include { get; set; } = new();

    public ChatGPTRequest AddTool(ChatGPTToolSpecification tool)
    {
        if (tool is null)
        {
            throw new ArgumentNullException(nameof(tool));
        }

        CustomTools.Add(tool);
        return this;
    }
}

public class ChatGPTToolSpecification
{
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
}

public class ChatGPTToolChoice
{
    public const string WebSearchPreview = "web_search_preview";
    public const string WebSearchPreview20250311 = "web_search_preview_2025_03_11";
    public const string WebSearch = "web_search";

    public string Type { get; set; } = "auto";
    public string? FunctionName { get; set; }
    public string? Mode { get; set; }
    public List<ChatGPTToolSpecification> AllowedTools { get; set; } = new();
}

public class ChatGPTReasoningOptions
{
    public string? Effort { get; set; }
    public string? Summary { get; set; }
}
