using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InstructSharp.Clients.ChatGPT;

internal sealed class ChatGPTImageGenerationResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public List<ChatGPTImageGenerationData> Data { get; set; } = new();

    [JsonPropertyName("revised_prompt")]
    public string? RevisedPrompt { get; set; }
}

internal sealed class ChatGPTImageGenerationData
{
    [JsonPropertyName("b64_json")]
    public string? Base64Payload { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("revised_prompt")]
    public string? RevisedPrompt { get; set; }
}

public sealed class ChatGPTGeneratedImage
{
    public string? Base64Data { get; init; }
    public string? Url { get; init; }
    public string? RevisedPrompt { get; init; }
}

public sealed class ChatGPTImageGenerationResult
{
    public string Model { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<ChatGPTGeneratedImage> Images { get; init; } = Array.Empty<ChatGPTGeneratedImage>();
}
