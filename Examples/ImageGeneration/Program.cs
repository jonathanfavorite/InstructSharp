using InstructSharp.Clients.ChatGPT;
using InstructSharp.Utils;

// Set OPENAI_API_KEY or replace this with your real API key.
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var client = new ChatGPTClient(apiKey);

string prompt = """
Intent: minimalist luxury construction showcase panorama. Projection first: TRUE seamless equirectangular 360-degree panorama, specifically designed for inside-out spherical viewing in Three.js. Left and right edges must connect perfectly with no visible seam. Stable horizon line. Balanced visual detail across the full wrap.

Scene design: create a seamless panoramic architectural environment made of multiple distinct construction scenes that gradually blend into each other around the 360 panorama. The viewer should feel surrounded by one connected premium construction world, with each zone clearly readable for interactive anchor placement.

Required scene zones distributed around the panorama:

- structural custom home framing zone: partially completed luxury house frame with exposed beams, framing lumber, roof trusses, architectural skeleton visible, clean construction site aesthetic, subtle work lighting, premium craftsmanship detail

- finished luxury custom home zone: completed modern Florida luxury home exterior, refined architecture, warm interior glow through glass, polished driveway, landscaping, understated elegance

- kitchen remodeling zone: high-end modern kitchen visible through open walls or large glass opening, premium cabinetry, stone countertops, pendant lighting, craftsmanship focus

- lanai / pool enclosure zone: elegant screened pool cage structure, pool reflections, refined patio furniture, enclosure framing clearly visible, tropical landscaping, outdoor luxury living atmosphere

- pergola / outdoor living craftsmanship zone: custom pergola, structural wood/metal detail, shaded patio, built-in outdoor kitchen or gathering space

- commercial construction zone: upscale restaurant or hospitality-style exterior, refined architecture, subtle signage-free facade, warm ambient architectural lighting, premium commercial build quality

- waterfront structural craftsmanship zone: dockside or waterfront lanai/deck detail, railings, custom structural work, calm water reflections

Transitions:
all zones must softly blend into one another using pathways, landscaping, lighting continuity, tropical vegetation, architectural sightlines, and environmental flow. No abrupt cutoffs or collage effect.

Visual style:
minimalist, premium, clean, elegant, restrained, architectural realism, Apple-level sophistication, cinematic but uncluttered, intentional negative space between major zones

Composition rules:
- no single hero subject
- no center-focused composition
- each scene zone should feel like its own “anchor” destination
- evenly distributed visual interest around full panorama
- leave subtle calm negative-space areas for UI overlays
- no visual clutter
- panoramic storytelling environment

Finishing details:
ultra-photorealistic, blue-hour dusk lighting, warm architectural glow, soft reflections, polished materials, concrete, wood, steel, glass, tropical Southwest Florida atmosphere, subtle haze, calm premium mood, no people, no text, no logos, no watermark

Camera:
ultra-wide TRUE equirectangular panoramic projection, seamless 360 wrap, horizon-centered, no fisheye collapse, no normal hero-shot framing, optimized specifically for interactive panoramic anchor placement.
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
