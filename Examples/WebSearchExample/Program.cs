using InstructSharp.Clients.ChatGPT;
using InstructSharp.Interfaces;

Console.WriteLine("### Web Search Demo\n");

ChatGPTClient client = new("YOUR-API-KEY-HERE");

ChatGPTRequest request = new()
{
    Model = ChatGPTModels.GPT5,
    Instructions = "You are a research assistant. Use web search to gather the latest information and cite your sources.",
    Input = "Summarize the most recent announcements about large-scale renewable energy investments.",
    EnableWebSearch = true
};

var response = await client.QueryAsync<string>(request);

Console.WriteLine("Response:\n");
Console.WriteLine(response.Result ?? "(no content returned)");
Console.WriteLine();
Console.WriteLine($"Model: {response.Model}");

if (response.Usage is { } usage)
{
    Console.WriteLine($"Prompt Tokens: {usage.PromptTokens}");
    Console.WriteLine($"Response Tokens: {usage.ResponseTokens}");
    Console.WriteLine($"Total Tokens: {usage.TotalTokens}");
}
