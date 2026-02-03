using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace InstructSharp.Clients.Gemini;

internal sealed class GeminiImageGenerationResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiImageCandidate> Candidates { get; set; } = new();
}

internal sealed class GeminiImageCandidate
{
    [JsonPropertyName("content")]
    public GeminiImageContent? Content { get; set; }
}

internal sealed class GeminiImageContent
{
    [JsonPropertyName("parts")]
    public List<GeminiImagePart> Parts { get; set; } = new();
}

internal sealed class GeminiImagePart
{
    [JsonPropertyName("inlineData")]
    public GeminiInlineData? InlineData { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

internal sealed class GeminiInlineData
{
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }
}

public sealed class GeminiGeneratedImage
{
    public string? Base64Data { get; init; }
    public string? MimeType { get; init; }
}

public sealed class GeminiImageGenerationResult
{
    public string Model { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<GeminiGeneratedImage> Images { get; init; } = Array.Empty<GeminiGeneratedImage>();
}
