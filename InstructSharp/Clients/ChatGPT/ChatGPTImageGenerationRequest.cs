using System;
using System.Collections.Generic;
using InstructSharp.Core;

namespace InstructSharp.Clients.ChatGPT;

public class ChatGPTImageGenerationRequest
{
    public string Model { get; set; } = ChatGPTModels.GPTImage1;
    public string Prompt { get; set; } = string.Empty;
    public string Size { get; set; } = ChatGPTImageParameters.Sizes.Square1024;
    public string Quality { get; set; } = ChatGPTImageParameters.Quality.Medium;
    public string? Style { get; set; }
    public string? OutputFormat { get; set; } = ChatGPTImageParameters.OutputFormats.Png;
    public string? Background { get; set; }
    public string? User { get; set; }
    public int ImageCount { get; set; } = 1;
    public int? Seed { get; set; }
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

        NormalizeTransparentBackground();
    }

    private void NormalizeTransparentBackground()
    {
        if (string.IsNullOrWhiteSpace(Background))
        {
            return;
        }

        if (!Background.Equals(ChatGPTImageParameters.Backgrounds.Transparent, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(OutputFormat))
        {
            OutputFormat = ChatGPTImageParameters.OutputFormats.Png;
            return;
        }

        bool isPng = OutputFormat.Equals(ChatGPTImageParameters.OutputFormats.Png, StringComparison.OrdinalIgnoreCase);
        bool isWebp = OutputFormat.Equals(ChatGPTImageParameters.OutputFormats.Webp, StringComparison.OrdinalIgnoreCase);
        if (!isPng && !isWebp)
        {
            throw new ArgumentException("Transparent background requires output_format png or webp.", nameof(OutputFormat));
        }
    }
}
