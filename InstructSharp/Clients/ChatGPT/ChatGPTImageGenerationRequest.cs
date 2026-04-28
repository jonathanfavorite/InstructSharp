using System;
using System.Collections.Generic;
using InstructSharp.Core;

namespace InstructSharp.Clients.ChatGPT;

public class ChatGPTImageGenerationRequest
{
    public string Model { get; set; } = ChatGPTModels.GPTImage2;
    public string Prompt { get; set; } = string.Empty;
    public string? Size { get; set; } = ChatGPTImageParameters.Sizes.Auto;
    public string? Quality { get; set; } = ChatGPTImageParameters.Quality.Auto;
    public string? Style { get; set; }
    public string? OutputFormat { get; set; } = ChatGPTImageParameters.OutputFormats.Png;
    public int? OutputCompression { get; set; }
    public string? Background { get; set; }
    public string? Moderation { get; set; }
    public string? ResponseFormat { get; set; }
    public int? PartialImages { get; set; }
    public bool? Stream { get; set; }
    public string? InputFidelity { get; set; }
    public string? User { get; set; }
    public int ImageCount { get; set; } = 1;
    public int? Seed { get; set; }
    public List<LLMImageRequest> Images { get; set; } = new();
    public List<ChatGPTImageReference> ImageReferences { get; set; } = new();
    public LLMImageRequest? Mask { get; set; }
    public ChatGPTImageReference? MaskReference { get; set; }

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

        if (ImageCount > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(ImageCount), "ImageCount must be between 1 and 10.");
        }

        if (OutputCompression is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(OutputCompression), "OutputCompression must be between 0 and 100.");
        }

        if (PartialImages is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(PartialImages), "PartialImages must be between 0 and 3.");
        }

        if (Images.Count > 0 && ImageReferences.Count > 0)
        {
            throw new ArgumentException("Use either Images or ImageReferences for image edits, not both.");
        }

        if ((Mask is not null || MaskReference is not null) && Images.Count == 0 && ImageReferences.Count == 0)
        {
            throw new ArgumentException("Image edits with a mask require at least one image.");
        }

        if (Mask is not null && MaskReference is not null)
        {
            throw new ArgumentException("Use either Mask or MaskReference for image edits, not both.");
        }

        foreach (var reference in ImageReferences)
        {
            reference.Validate();
        }

        MaskReference?.Validate();
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

public sealed class ChatGPTImageReference
{
    public string? FileId { get; set; }
    public string? ImageUrl { get; set; }

    public static ChatGPTImageReference FromFileId(string fileId) => new() { FileId = fileId };

    public static ChatGPTImageReference FromImageUrl(string imageUrl) => new() { ImageUrl = imageUrl };

    public void Validate()
    {
        bool hasFileId = !string.IsNullOrWhiteSpace(FileId);
        bool hasImageUrl = !string.IsNullOrWhiteSpace(ImageUrl);
        if (hasFileId == hasImageUrl)
        {
            throw new ArgumentException("Image references must set exactly one of FileId or ImageUrl.");
        }
    }
}
