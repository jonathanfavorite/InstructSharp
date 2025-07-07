using InstructSharp.Clients.ChatGPT;
using InstructSharp.Core;


bool stream = true;

ChatGPTClient client = new("YOUR-API-KEY");
ChatGPTRequest request = new()
{
    Instructions = "You are a helpful assistant",
    Input = "Write a brief story about a cowardly king going on an adventure to discover his inner courage.",
    Model = ChatGPTModels.GPT4oMini,
    Stream = stream // Set to true for streaming responses
};


if (stream)
{
    // receive the response in chunks from the stream
    await foreach (var chunk in client.StreamQueryAsync<string>(request))
    {
        Console.Write(chunk);
    }
}
else
{
    LLMResponse<string> response = await client.QueryAsync<string>(request);
    Console.WriteLine(response.Result);
}


Console.ReadLine();