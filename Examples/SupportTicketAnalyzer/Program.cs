using InstructSharp.Clients.ChatGPT;
using InstructSharp.Clients.Claude;
using InstructSharp.Clients.DeepSeek;
using InstructSharp.Clients.Gemini;
using InstructSharp.Clients.Grok;
using InstructSharp.Clients.LLama;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.ComponentModel;

string instructions = "You are a customer support AI analyst. Analyze the customer's message and provide structured insights to help resolve their issue efficiently.";

string input = @"I've been trying to access my account for the past 2 hours and keep getting an error message that says 
'Invalid credentials' even though I'm 100% sure my password is correct. I even tried resetting it twice through your 
password reset link, but the new password doesn't work either! This is extremely frustrating as I have important 
documents I need to access for a meeting in 30 minutes. I've been a premium customer for 3 years and never had this 
issue before. The error code showing is ERR_AUTH_1042. Can someone please help me urgently? I tried calling support 
but the wait time is over 45 minutes which I don't have. My username is john.doe@email.com.";


// Comment out the models you don't want to use
List<ILLMClient> clients = new()
{
    new ChatGPTClient("YOUR-API-KEY-HERE"),
    new ClaudeClient("YOUR-API-KEY-HERE"),
    new LLamaClient("YOUR-API-KEY-HERE"),
    new GeminiClient("YOUR-API-KEY-HERE"),
    new DeepSeekClient("YOUR-API-KEY-HERE"),
    new GrokClient("YOUR-API-KEY-HERE"),
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
    var response = await client.QueryAsync<SupportTicketAnalysis>(request);
    Console.Write($"Done!");
    Console.WriteLine();
    DisplayAnalysis(request.Model, response.Result);
}

void DisplayAnalysis(string model, SupportTicketAnalysis? ticket)
{
    if(ticket is null)
    {
        Console.WriteLine($"### {model} could not map the response to a ticket.");
        return;
    }
    Console.WriteLine();
    Console.WriteLine("##### Retrieved Ticket");
    Console.WriteLine($"**Model:** {model}");
    Console.WriteLine($"**Issue Category:** {ticket.IssueCategory}");
    Console.WriteLine($"**Urgency:** {ticket.Urgency}");
    Console.WriteLine($"**Customer Sentiment:** {ticket.CustomerSentiment}");
    Console.WriteLine($"**Product Mentioned:** {ticket.ProductMentioned}");
    Console.WriteLine($"**Problem Summary:** {ticket.ProblemSummary}");
    Console.WriteLine($"**Suggested Actions:** {string.Join(", ", ticket.SuggestedActions)}");
    Console.WriteLine($"**Requires Escalation:** {ticket.RequiresEscalation}");
    Console.WriteLine($"**Relevant Resources:** {string.Join(", ", ticket.RelevantResources)}");

    Console.WriteLine();
    Console.WriteLine();
}

class SupportTicketAnalysis
{
    [Description("Primary category of the issue (Technical, Billing, Feature Request, Bug Report, Other)")]
    public string IssueCategory { get; set; }

    [Description("Urgency level based on content (Critical, High, Medium, Low)")]
    public string Urgency { get; set; }

    [Description("Customer's emotional tone (Frustrated, Neutral, Happy, Angry, Confused)")]
    public string CustomerSentiment { get; set; }

    [Description("Specific product or service mentioned")]
    public string ProductMentioned { get; set; }

    [Description("Core problem in one sentence")]
    public string ProblemSummary { get; set; }

    [Description("Suggested resolution steps")]
    public List<string> SuggestedActions { get; set; }

    [Description("Whether this requires escalation to a human agent")]
    public bool RequiresEscalation { get; set; }

    [Description("Relevant knowledge base articles or documentation to reference")]
    public List<string> RelevantResources { get; set; }
}