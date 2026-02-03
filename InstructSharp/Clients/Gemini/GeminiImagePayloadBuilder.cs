using System;
using System.Collections.Generic;

namespace InstructSharp.Clients.Gemini;

internal static class GeminiImagePayloadBuilder
{
    private static readonly HashSet<string> ModelsWithImageSize = new(StringComparer.OrdinalIgnoreCase)
    {
        GeminiModels.Gemini3ImagePreview
    };

    public static Dictionary<string, object?>? BuildGenerationConfig(GeminiImageGenerationRequest request)
    {
        var generationConfig = new Dictionary<string, object?>();

        var responseModalities = NormalizeResponseModalities(request.ResponseModalities);
        if (responseModalities.Count > 0)
        {
            generationConfig["responseModalities"] = responseModalities;
        }

        var imageConfig = BuildImageConfig(request);
        if (imageConfig is { Count: > 0 })
        {
            generationConfig["imageConfig"] = imageConfig;
        }

        if (request.ImageCount > 1)
        {
            generationConfig["candidateCount"] = request.ImageCount;
        }

        return generationConfig.Count > 0 ? generationConfig : null;
    }

    private static Dictionary<string, object?>? BuildImageConfig(GeminiImageGenerationRequest request)
    {
        var imageConfig = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(request.AspectRatio))
        {
            imageConfig["aspectRatio"] = request.AspectRatio;
        }

        if (SupportsImageSize(request.Model) && !string.IsNullOrWhiteSpace(request.ImageSize))
        {
            imageConfig["imageSize"] = request.ImageSize;
        }

        return imageConfig.Count > 0 ? imageConfig : null;
    }

    private static bool SupportsImageSize(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        return ModelsWithImageSize.Contains(model);
    }

    private static List<string> NormalizeResponseModalities(IReadOnlyList<string>? modalities)
    {
        if (modalities is null || modalities.Count == 0)
        {
            return new List<string>();
        }

        var normalized = new List<string>(modalities.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var modality in modalities)
        {
            if (string.IsNullOrWhiteSpace(modality))
            {
                continue;
            }

            var trimmed = modality.Trim();
            string mapped = trimmed;

            if (trimmed.Equals("IMAGE", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "Image";
            }
            else if (trimmed.Equals("TEXT", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                mapped = "Text";
            }

            if (seen.Add(mapped))
            {
                normalized.Add(mapped);
            }
        }

        return normalized;
    }
}
