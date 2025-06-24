using InstructSharp.Clients.ChatGPT;
using InstructSharp.Clients.Claude;
using InstructSharp.Clients.DeepSeek;
using InstructSharp.Clients.Gemini;
using InstructSharp.Clients.Grok;
using InstructSharp.Clients.LLama;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.ComponentModel;

string instructions = "You are an expert product review analyst. Analyze the given customer review and extract structured insights.";

string input = @"I bought this laptop last month and I have mixed feelings. The performance is absolutely fantastic - 
it handles all my development work and even some light gaming without breaking a sweat. The 16GB RAM and fast SSD make 
multitasking a breeze. However, the battery life is disappointing, lasting only about 4 hours with normal use. 
The keyboard feels great for typing, but the trackpad is a bit finicky sometimes. The display is gorgeous with 
vibrant colors, perfect for photo editing. Build quality seems solid, though it's heavier than I expected. 
For the price, I think there are better options available, but if performance is your main concern, this could work.";


// Comment out the models you don't want to use
List<ILLMClient> clients = new()
{
    new ChatGPTClient("YOUR-API-KEY-HERE"),
    new ClaudeClient("YOUR-API-KEY-HERE"),
    new GeminiClient("YOUR-API-KEY-HERE"),
    new DeepSeekClient("YOUR-API-KEY-HERE"),
    new GrokClient("YOUR-API-KEY-HERE"),
    new LLamaClient("YOUR-API-KEY-HERE"),
};

Console.WriteLine("Instructions: " + instructions);
Console.WriteLine();
Console.WriteLine("Input: " + input);
Console.WriteLine();
Console.WriteLine();

Dictionary<LLMProvider, ILLMRequest> requests = new()
{
    [LLMProvider.ChatGPT] = new ChatGPTRequest { Model = ChatGPTModels.GPT4o },
    [LLMProvider.Claude] = new ClaudeRequest { Model = ClaudeModels.ClaudeSonnet37_Latest },
    [LLMProvider.LLama] = new LLamaRequest { Model = LlamaModels.Llama4_Maverick_17B_128E_Instruct_FP8 },
    [LLMProvider.Gemini] = new GeminiRequest { Model = GeminiModels.Gemini25Flash },
    [LLMProvider.DeepSeek] = new DeepSeekRequest { Model = DeepSeekModels.DeepSeekChat },
    [LLMProvider.Grok] = new GrokRequest { Model = GrokModels.Grok3 }
};

foreach (ILLMClient client in clients)
{
    ILLMRequest request = requests[client.GetLLMProvider()];
    request.Instructions = instructions;
    request.Input = input;

    Console.Write($"### {request.Model} is thinking...");
    var response = await client.QueryAsync<ProductReviewAnalysis>(request);
    Console.Write($"Done!");
    Console.WriteLine();
    DisplayAnalysis(request.Model, response.Result);
}

void DisplayAnalysis(string model, ProductReviewAnalysis? ticket)
{
    if (ticket is null)
    {
        Console.WriteLine($"### {model} could not map the response to a ticket.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine("##### Retrieved Review Analysis");
    Console.WriteLine($"**Model:** {model}");
    Console.WriteLine($"**Sentiment:** {ticket.Sentiment}");
    Console.WriteLine($"**Confidence:** {ticket.Confidence:F2}");
    Console.WriteLine($"**Positives:** {string.Join(", ", ticket.Positives)}");
    Console.WriteLine($"**Negatives:** {string.Join(", ", ticket.Negatives)}");
    Console.WriteLine($"**Features Discussed:** {string.Join(", ", ticket.FeaturesDiscussed)}");
    Console.WriteLine($"**Would Recommend:** {ticket.WouldRecommend}");
    Console.WriteLine($"**Estimated Rating:** {ticket.EstimatedRating}");

    Console.WriteLine();
    Console.WriteLine();
}

class ProductReviewAnalysis
{
    [Description("Overall sentiment of the review (Positive, Negative, Neutral, Mixed)")]
    public string Sentiment { get; set; }

    [Description("Confidence score of the sentiment analysis from 0.0 to 1.0")]
    public double Confidence { get; set; }

    [Description("Key positive aspects mentioned in the review")]
    public List<string> Positives { get; set; }

    [Description("Key negative aspects or complaints mentioned")]
    public List<string> Negatives { get; set; }

    [Description("Main product features discussed in the review")]
    public List<string> FeaturesDiscussed { get; set; }

    [Description("Whether the reviewer would recommend this product")]
    public bool WouldRecommend { get; set; }

    [Description("Estimated star rating based on the review content (1-5)")]
    public int EstimatedRating { get; set; }
}