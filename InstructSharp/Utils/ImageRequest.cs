using InstructSharp.Core;

namespace InstructSharp.Utils;

public static class ImageRequest
{
    public static LLMImageRequest FromFile(string filePath, int detailRequired = 1)
    {
        return new LLMImageRequest(ToDataUrl(filePath), detailRequired);
    }

    public static List<LLMImageRequest> FromFiles(IEnumerable<string> filePaths, int detailRequired = 1)
    {
        if (filePaths is null)
        {
            throw new ArgumentNullException(nameof(filePaths));
        }

        return filePaths.Select(filePath => FromFile(filePath, detailRequired)).ToList();
    }

    public static async Task<LLMImageRequest> FromFileAsync(string filePath, int detailRequired = 1, CancellationToken cancellationToken = default)
    {
        return new LLMImageRequest(await ToDataUrlAsync(filePath, cancellationToken), detailRequired);
    }

    public static async Task<List<LLMImageRequest>> FromFilesAsync(IEnumerable<string> filePaths, int detailRequired = 1, CancellationToken cancellationToken = default)
    {
        if (filePaths is null)
        {
            throw new ArgumentNullException(nameof(filePaths));
        }

        var images = new List<LLMImageRequest>();
        foreach (string filePath in filePaths)
        {
            images.Add(await FromFileAsync(filePath, detailRequired, cancellationToken));
        }

        return images;
    }

    public static string ToBase64(string filePath)
    {
        ValidateImagePath(filePath);
        return Convert.ToBase64String(File.ReadAllBytes(filePath));
    }

    public static async Task<string> ToBase64Async(string filePath, CancellationToken cancellationToken = default)
    {
        ValidateImagePath(filePath);
        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        return Convert.ToBase64String(bytes);
    }

    public static string ToDataUrl(string filePath)
    {
        string contentType = GetContentType(filePath);
        return $"data:{contentType};base64,{ToBase64(filePath)}";
    }

    public static async Task<string> ToDataUrlAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string contentType = GetContentType(filePath);
        string base64 = await ToBase64Async(filePath, cancellationToken);
        return $"data:{contentType};base64,{base64}";
    }

    public static string GetContentType(string filePath)
    {
        ValidateImagePath(filePath);

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => throw new NotSupportedException($"Unsupported image file extension '{extension}'. Supported extensions are .png, .jpg, .jpeg, .webp, and .gif.")
        };
    }

    private static void ValidateImagePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Image path is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Image file not found: {filePath}", filePath);
        }
    }
}
