using InstructSharp.Clients.ChatGPT;

// Replace this with your real API key or set the OPENAI_API_KEY environment variable.
string apiKey = "YOUR-API-KEY-HERE";
var client = new ChatGPTClient(apiKey);

string prompt = """
your prompt here
""";

var request = new ChatGPTImageGenerationRequest
{
    Prompt = prompt,
    Model = ChatGPTModels.GPTImage1,
    Size = ChatGPTImageParameters.Sizes.Landscape1536x1024,
    Quality = ChatGPTImageParameters.Quality.Low,
    OutputFormat = ChatGPTImageParameters.OutputFormats.Png,
    ImageCount = 1,
    User = "image-generation-example-3",
};

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
        string fileName = Path.Combine(outputDirectory, $"cityscape_{i + 1}.png");
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