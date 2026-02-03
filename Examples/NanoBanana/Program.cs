using InstructSharp.Clients.Gemini;

// Replace this with your real API key or set the GOOGLE_API_KEY environment variable.
string apiKey = "YOUR-API-KEY";
var client = new GeminiClient(apiKey);

string prompt = """
Creamy Cilantro-Lime Chicken Quesadillas, featuring golden-brown corn tortillas filled with juicy, seasoned chicken, melted cheddar, and a rich cilantro-lime sauce. Set on a rustic wooden board, this Mexican-inspired dish captures the essence of a warm, inviting dinner. The composition is overhead, showcasing the quesadilla cut into wedges, revealing the vibrant filling. Soft natural window light casts gentle shadows, enhancing the textures and colors of the dish. The mood is cozy and appetizing, with a shallow depth of field creating creamy bokeh in the background. An ultra photorealistic style ensures true-to-life colors, ideal for a cookbook cover.
""";

var request = new GeminiImageGenerationRequest
{
    Prompt = prompt,
    Model = GeminiModels.Gemini25FlashImage,
    AspectRatio = GeminiImageParameters.AspectRatios.Landscape4x3,
    ImageCount = 1,
    ResponseModalities = new() { GeminiImageParameters.ResponseModalities.Image }
};

Console.WriteLine("Sending prompt to Gemini image API...");
var result = await client.GenerateImageAsync(request);

Console.WriteLine($"Model: {result.Model}");
Console.WriteLine($"Created: {result.CreatedAt:O}");
Console.WriteLine();

string outputDirectory = Path.Combine(AppContext.BaseDirectory, "outputs", "nanobanana");
Directory.CreateDirectory(outputDirectory);

for (int i = 0; i < result.Images.Count; i++)
{
    var image = result.Images[i];
    string label = $"Image #{i + 1}";

    if (!string.IsNullOrEmpty(image.Base64Data))
    {
        string extension = GetExtensionFromMimeType(image.MimeType);
        string fileName = Path.Combine(outputDirectory, $"nano_banana_{i + 1}.{extension}");
        byte[] bytes = Convert.FromBase64String(image.Base64Data);
        await File.WriteAllBytesAsync(fileName, bytes);
        Console.WriteLine($"{label} saved to {fileName}");
    }

    Console.WriteLine();
}

Console.WriteLine("Done!");

static string GetExtensionFromMimeType(string? mimeType)
{
    if (string.IsNullOrWhiteSpace(mimeType))
    {
        return "png";
    }

    return mimeType.ToLowerInvariant() switch
    {
        "image/png" => "png",
        "image/jpeg" => "jpg",
        "image/webp" => "webp",
        "image/gif" => "gif",
        _ => "png"
    };
}
