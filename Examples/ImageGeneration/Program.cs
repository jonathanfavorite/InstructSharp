using InstructSharp.Clients.ChatGPT;
using InstructSharp.Utils;

// Set OPENAI_API_KEY or replace this with your real API key.
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var client = new ChatGPTClient(apiKey);

string prompt = """
Intent: fantasy environment panorama . Background: expansive equirectangular 360-degree landscape with distant jagged mountains, stormy sky gradients, and a massive ancient castle on a far horizon ridge. Foreground: dense dark forest with twisted trees, glowing mushrooms, scattered ruins, and winding paths fading into shadow. Hero subject: towering gothic castle with spires and faint glowing windows, partially shrouded in mist. Finishing details: high-detail fantasy realism, atmospheric fog layers, subtle magical particles, dramatic lighting contrast, no logos or trademarks, no watermark. Atmosphere: moody, mystical, slightly ominous with soft volumetric light rays breaking through clouds. Camera: ultra-wide panoramic equirectangular projection, seamless 360 wrap, cinematic composition.
""";


var request = new ChatGPTImageGenerationRequest
{
    Prompt = prompt,
    Model = ChatGPTModels.GPTImage2,
    Size = ChatGPTImageParameters.Sizes.Landscape2048x1152,
    Quality = ChatGPTImageParameters.Quality.High,
    OutputFormat = ChatGPTImageParameters.OutputFormats.Png,
    Background = ChatGPTImageParameters.Backgrounds.Auto,
    Moderation = ChatGPTImageParameters.Moderation.Auto,
    OutputCompression = null, // 0-100; only applies to jpeg/webp.
    PartialImages = 0, // 0-3 when Stream is true.
    ResponseFormat = null, // DALL-E only: "url" or "b64_json"; GPT Image returns base64.
    Stream = false,
    Style = null, // DALL-E 3 only: "vivid" or "natural".
    InputFidelity = null, // Edit endpoint only: "high" or "low".
    ImageCount = 1,
    User = "image-generation-example-gpt-image-2",
};

//await request.AddImageFileAsync(@"C:\example\image.jpg"); // if you wanted to pass a refernce image

Console.WriteLine("Sending prompt to ChatGPT image API...");
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
        string extension = request.OutputFormat ?? ChatGPTImageParameters.OutputFormats.Png;
        string fileName = Path.Combine(outputDirectory, $"{Guid.NewGuid().ToString()}.{extension}");
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
