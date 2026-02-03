using System;
using System.Collections.Generic;
using InstructSharp.Core;

namespace InstructSharp.Clients.Gemini;

public sealed class GeminiImageGenerationRequest
{
    public string Model { get; set; } = GeminiModels.Gemini3ImagePreview;
    public string Prompt { get; set; } = string.Empty;
    public List<string> ResponseModalities { get; set; } = new();
    public string? AspectRatio { get; set; } = GeminiImageParameters.AspectRatios.Square1x1;
    public string? ImageSize { get; set; }
    public int ImageCount { get; set; } = 1;
    public List<LLMImageRequest> Images { get; set; } = new();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            throw new ArgumentException("Prompt is required for image generation.", nameof(Prompt));
        }

        if (ImageCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ImageCount), "ImageCount must be at least 1.");
        }
    }
}
