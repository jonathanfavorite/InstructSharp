using InstructSharp.Clients.ChatGPT;
using InstructSharp.Utils;

// Replace this with your real API key or set the OPENAI_API_KEY environment variable.
string apiKey = "YOUR-API-KEY";
var client = new ChatGPTClient(apiKey);

string imagePath = @"C:\test\instructsharp\example1.jpg";
string secondImagePath = @"C:\test\instructsharp\example2.png";
string prompt = @"extract the main subject and redraw it with clean cartoon styling on a transparent background";




var request = new ChatGPTImageGenerationRequest
{
    Prompt = prompt,
    Model = ChatGPTModels.GPTImage1,
    Size = ChatGPTImageParameters.Sizes.Landscape1536x1024,
    Quality = ChatGPTImageParameters.Quality.Low,
    OutputFormat = ChatGPTImageParameters.OutputFormats.Png,
    Background = ChatGPTImageParameters.Backgrounds.Transparent, //////////// THIS IS FOR TRANSPARENT BACKGROUND RESULT
    ImageCount = 1,
    User = "image-generation-with-image-supplied",
    Images = ImageRequest.FromFiles(new[] { imagePath, secondImagePath })
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
