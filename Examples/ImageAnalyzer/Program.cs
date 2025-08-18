using InstructSharp.Clients.ChatGPT;
using InstructSharp.Core;
using static System.Net.Mime.MediaTypeNames;

string location = @"C:\test\some_image.png";

string base64 = GetBase64OfImageFromPath(location);

ChatGPTClient client = new("");
ChatGPTRequest request = new()
{
    Instructions = "your instruction",
    Input = "your-input",
    Model = "gpt-5-nano",
    Images = new()
    {
        new LLMImageRequest($"data:image/png;base64,{base64}")
    }
};

LLMResponse<string> response = await client.QueryAsync<string>(request);
Console.WriteLine(response.Result);


Console.ReadLine();

string GetBase64OfImageFromPath(string imagePath)
{
    if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        throw new ArgumentException("Image path is invalid or file does not exist.", nameof(imagePath));

    byte[] imageBytes = File.ReadAllBytes(imagePath);
    return Convert.ToBase64String(imageBytes);
}