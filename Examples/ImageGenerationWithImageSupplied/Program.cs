using InstructSharp.Clients.ChatGPT;
using InstructSharp.Core;

// Replace this with your real API key or set the OPENAI_API_KEY environment variable.
string apiKey = "YOUR-API-KEY";
var client = new ChatGPTClient(apiKey);

string imagePath = @"C:\some\image_path\image.jpg";
string prompt = @"remake this image but in cartoon format";




string base64 = GetBase64OfImageFromPath(imagePath);
string contentType = GetContentTypeFromPath(imagePath);
string dataUrl = $"data:{contentType};base64,{base64}";

var request = new ChatGPTImageGenerationRequest
{
    Prompt = prompt,
    Model = ChatGPTModels.GPTImage1,
    Size = ChatGPTImageParameters.Sizes.Landscape1536x1024,
    Quality = ChatGPTImageParameters.Quality.Low,
    OutputFormat = ChatGPTImageParameters.OutputFormats.Png,
    ImageCount = 1,
    User = "image-generation-with-image-supplied",
    Images = new()
    {
        new LLMImageRequest(dataUrl)
        // You can also pass a local file path directly:
        // new LLMImageRequest(imagePath)
    }
};

Console.WriteLine("Sending prompt to ChatGPT image API with an input image...");
var result = await client.GenerateImageAsync(request);

Console.WriteLine($"Model: {result.Model}");
Console.WriteLine($"Created: {result.CreatedAt:O}");
Console.WriteLine();

string outputDirectory = Path.Combine(AppContext.BaseDirectory, "outputs");
Directory.CreateDirectory(outputDirectory);

for (int i = 0; i < result.Images.Count; i++)
{
    var image = result.Images[i];
    string label = $"Image #{i + 1}";

    if (!string.IsNullOrEmpty(image.Base64Data))
    {
        string fileName = Path.Combine(outputDirectory, $"described_{i + 1}.png");
        byte[] bytes = Convert.FromBase64String(image.Base64Data);
        await File.WriteAllBytesAsync(fileName, bytes);
        Console.WriteLine($"{label} saved to {fileName}");
    }
    else if (!string.IsNullOrEmpty(image.Url))
    {
        Console.WriteLine($"{label} URL: {image.Url}");
    }

    if (!string.IsNullOrWhiteSpace(image.RevisedPrompt))
    {
        Console.WriteLine($"Revised prompt: {image.RevisedPrompt}");
    }

    Console.WriteLine();
}

Console.WriteLine("Done!");

static string GetBase64OfImageFromPath(string imagePath)
{
    if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
    {
        throw new ArgumentException("Image path is invalid or file does not exist.", nameof(imagePath));
    }

    byte[] imageBytes = File.ReadAllBytes(imagePath);
    return Convert.ToBase64String(imageBytes);
}

static string GetContentTypeFromPath(string imagePath)
{
    string extension = Path.GetExtension(imagePath).ToLowerInvariant();
    return extension switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
