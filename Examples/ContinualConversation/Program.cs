using InstructSharp.Clients.ChatGPT;
using System.Collections.Generic;

Console.WriteLine("### Continual Conversation Demo\n");

ChatGPTClient client = new("YOUR-API-KEY-HERE");

ChatGPTConversation conversation = await client.CreateConversationAsync(new ChatGPTConversationCreateRequest
{
    Metadata = new Dictionary<string, string>
    {
        ["topic"] = "continual-conversation-demo"
    }
});

Console.WriteLine($"Conversation Id: {conversation.Id}");
Console.WriteLine();

ChatGPTRequest firstRequest = new()
{
    Model = ChatGPTModels.GPT4o,
    Instructions = "You are a helpful assistant.",
    Input = "My name is Jeff.",
    ConversationId = conversation.Id
};

var firstResponse = await client.QueryAsync<string>(firstRequest);

Console.WriteLine("Assistant (turn 1):");
Console.WriteLine(firstResponse.Result ?? "(no content returned)");
Console.WriteLine();

Console.WriteLine("Reusing the same conversation id...\n");

ChatGPTRequest secondRequest = new()
{
    Model = ChatGPTModels.GPT4o,
    Instructions = "You are a helpful assistant.",
    Input = "What is my name?",
    ConversationId = conversation.Id
};

var secondResponse = await client.QueryAsync<string>(secondRequest);

Console.WriteLine("Assistant (turn 2):");
Console.WriteLine(secondResponse.Result ?? "(no content returned)");
Console.WriteLine();
Console.WriteLine($"Model: {secondResponse.Model}");

if (secondResponse.Usage is { } usage)
{
    Console.WriteLine($"Prompt Tokens: {usage.PromptTokens}");
    Console.WriteLine($"Response Tokens: {usage.ResponseTokens}");
    Console.WriteLine($"Total Tokens: {usage.TotalTokens}");
}
