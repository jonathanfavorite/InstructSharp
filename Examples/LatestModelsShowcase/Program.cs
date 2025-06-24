using InstructSharp.Clients.ChatGPT;
using InstructSharp.Clients.Claude;
using InstructSharp.Clients.DeepSeek;
using InstructSharp.Clients.Gemini;
using InstructSharp.Clients.Grok;
using InstructSharp.Clients.LLama;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.ComponentModel;

string instructions = "You are an expert AI assistant. Analyze the given text and provide comprehensive insights about its content, sentiment, and key themes.";

string input = @"The rapid advancement of artificial intelligence has transformed how we approach problem-solving across industries. 
From healthcare diagnostics to autonomous vehicles, AI systems are becoming increasingly sophisticated and integrated into our daily lives. 
However, this progress also raises important questions about ethics, privacy, and the future of human work. 
As we develop more capable AI systems, we must carefully consider the balance between innovation and responsibility.";

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
    [LLMProvider.ChatGPT] = new ChatGPTRequest { Model = ChatGPTModels.GPT45Preview },
    [LLMProvider.Claude] = new ClaudeRequest { Model = ClaudeModels.ClaudeOpus4_20250514 },
    [LLMProvider.Gemini] = new GeminiRequest { Model = GeminiModels.Gemini25Pro },
    [LLMProvider.DeepSeek] = new DeepSeekRequest { Model = DeepSeekModels.DeepSeekChat },
    [LLMProvider.Grok] = new GrokRequest { Model = GrokModels.Grok3 },
    [LLMProvider.LLama] = new LLamaRequest { Model = LlamaModels.Llama4_Maverick_17B_128E_Instruct_FP8 },
};

foreach (ILLMClient client in clients)
{
    ILLMRequest request = requests[client.GetLLMProvider()];
    request.Instructions = instructions;
    request.Input = input;

    Console.Write($"### {request.Model} is analyzing...");
    var response = await client.QueryAsync<TextAnalysis>(request);
    Console.Write($"Done!");
    Console.WriteLine();
    DisplayAnalysis(request.Model, response.Result);
}

void DisplayAnalysis(string model, TextAnalysis? analysis)
{
    if (analysis is null)
    {
        Console.WriteLine($"### {model} could not map the response to analysis.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine("##### Retrieved Text Analysis");
    Console.WriteLine($"**Model:** {model}");
    Console.WriteLine($"**Overall Sentiment:** {analysis.OverallSentiment}");
    Console.WriteLine($"**Confidence:** {analysis.Confidence:F2}");
    Console.WriteLine($"**Key Themes:** {string.Join(", ", analysis.KeyThemes)}");
    Console.WriteLine($"**Main Topics:** {string.Join(", ", analysis.MainTopics)}");
    Console.WriteLine($"**Complexity Level:** {analysis.ComplexityLevel}");
    Console.WriteLine($"**Readability Score:** {analysis.ReadabilityScore:F1}");
    Console.WriteLine($"**Key Insights:** {string.Join(", ", analysis.KeyInsights)}");
    Console.WriteLine($"**Potential Applications:** {string.Join(", ", analysis.PotentialApplications)}");
    Console.WriteLine($"**Risk Factors:** {string.Join(", ", analysis.RiskFactors)}");

    Console.WriteLine();
    Console.WriteLine();
}

class TextAnalysis
{
    [Description("Overall sentiment of the text (Positive, Negative, Neutral, Mixed)")]
    public string OverallSentiment { get; set; }

    [Description("Confidence in the analysis from 0.0 to 1.0")]
    public double Confidence { get; set; }

    [Description("Primary themes identified in the text")]
    public List<string> KeyThemes { get; set; }

    [Description("Main topics or subjects discussed")]
    public List<string> MainTopics { get; set; }

    [Description("Complexity level of the content (Simple, Moderate, Complex, Expert)")]
    public string ComplexityLevel { get; set; }

    [Description("Readability score (0-100, higher is more readable)")]
    public double ReadabilityScore { get; set; }

    [Description("Key insights or conclusions drawn from the text")]
    public List<string> KeyInsights { get; set; }

    [Description("Potential real-world applications of the concepts discussed")]
    public List<string> PotentialApplications { get; set; }

    [Description("Risk factors or concerns mentioned in the text")]
    public List<string> RiskFactors { get; set; }
} 