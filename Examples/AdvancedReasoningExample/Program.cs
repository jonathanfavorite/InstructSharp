using InstructSharp.Clients.ChatGPT;
using InstructSharp.Clients.Claude;
using InstructSharp.Clients.DeepSeek;
using InstructSharp.Clients.Gemini;
using InstructSharp.Clients.Grok;
using InstructSharp.Clients.LLama;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.ComponentModel;

string instructions = "You are an expert mathematical and logical reasoning assistant. Solve the given problem step by step, showing your work and reasoning process.";

string input = @"A company has 100 employees. 60% of them are engineers, 25% are designers, and the rest are managers. 
Among the engineers, 40% have a master's degree. Among the designers, 30% have a master's degree. 
Among the managers, 80% have a master's degree. 

What percentage of the total company has a master's degree? 
Show your step-by-step calculation.";

// Comment out the models you don't want to use
List<ILLMClient> clients = new()
{
    new ChatGPTClient("YOUR-API-KEY-HERE"),
    new ClaudeClient("YOUR-API-KEY-HERE"),
    new DeepSeekClient("YOUR-API-KEY-HERE"),
};

Console.WriteLine("Instructions: " + instructions);
Console.WriteLine();
Console.WriteLine("Input: " + input);
Console.WriteLine();
Console.WriteLine();

Dictionary<LLMProvider, ILLMRequest> requests = new()
{
    [LLMProvider.ChatGPT] = new ChatGPTRequest { Model = ChatGPTModels.O1Preview },
    [LLMProvider.Claude] = new ClaudeRequest { Model = ClaudeModels.ClaudeSonnet37_Latest },
    [LLMProvider.DeepSeek] = new DeepSeekRequest { Model = DeepSeekModels.DeepSeekReasoner },
};

foreach (ILLMClient client in clients)
{
    ILLMRequest request = requests[client.GetLLMProvider()];
    request.Instructions = instructions;
    request.Input = input;

    Console.Write($"### {request.Model} is reasoning...");
    var response = await client.QueryAsync<MathematicalReasoning>(request);
    Console.Write($"Done!");
    Console.WriteLine();
    DisplayReasoning(request.Model, response.Result);
}

void DisplayReasoning(string model, MathematicalReasoning? reasoning)
{
    if (reasoning is null)
    {
        Console.WriteLine($"### {model} could not map the response to reasoning.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine("##### Retrieved Reasoning Analysis");
    Console.WriteLine($"**Model:** {model}");
    Console.WriteLine($"**Final Answer:** {reasoning.FinalAnswer}");
    Console.WriteLine($"**Confidence:** {reasoning.Confidence:F2}");
    Console.WriteLine($"**Problem Type:** {reasoning.ProblemType}");
    Console.WriteLine($"**Steps Used:** {reasoning.StepsUsed}");
    Console.WriteLine($"**Key Calculations:** {string.Join(", ", reasoning.KeyCalculations)}");
    Console.WriteLine($"**Reasoning Quality:** {reasoning.ReasoningQuality}");
    Console.WriteLine($"**Time Complexity:** {reasoning.TimeComplexity}");
    Console.WriteLine($"**Alternative Approaches:** {string.Join(", ", reasoning.AlternativeApproaches)}");

    Console.WriteLine();
    Console.WriteLine();
}

class MathematicalReasoning
{
    [Description("The final numerical answer to the problem")]
    public double FinalAnswer { get; set; }

    [Description("Confidence in the answer from 0.0 to 1.0")]
    public double Confidence { get; set; }

    [Description("Type of mathematical problem (Percentage, Algebra, Geometry, etc.)")]
    public string ProblemType { get; set; }

    [Description("Number of reasoning steps used to solve the problem")]
    public int StepsUsed { get; set; }

    [Description("Key mathematical calculations performed")]
    public List<string> KeyCalculations { get; set; }

    [Description("Quality of reasoning process (Excellent, Good, Fair, Poor)")]
    public string ReasoningQuality { get; set; }

    [Description("Time complexity of the solution approach")]
    public string TimeComplexity { get; set; }

    [Description("Alternative approaches that could be used")]
    public List<string> AlternativeApproaches { get; set; }
} 