using InstructSharp.Clients.ChatGPT;
using InstructSharp.Core;
using System.IO;
using System.Text.Json;


bool stream = true;
bool dumpRawEvents = true;
string? rawEventLogPath = null;
StreamWriter? rawEventWriter = null;

if (dumpRawEvents)
{
    rawEventLogPath = Path.Combine(Path.GetTempPath(), $"chatgpt-events-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.jsonl");
    rawEventWriter = new StreamWriter(File.Open(rawEventLogPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
    Console.WriteLine($"[debug] Raw ChatGPT events -> {rawEventLogPath}");
}

ChatGPTClient client = new("API-KEY-HERE");
ChatGPTRequest request = new()
{
    Instructions = "Act as an expert recipe generator and normalizer. The Input will be plain text that is either (a) raw recipe text copied from somewhere or (b) a natural-language request like “give me a salty umami crock pot roast” (even if the user says “recipes,” always return just one best-fit recipe). Always produce exactly one complete recipe that fits the structured output type, filling title, description, cuisine, prep/cook minutes, servings, ingredients, and ordered steps. If the input already contains a recipe, cleanly extract and normalize it without changing its intended flavor or method, ignoring blog chatter and unrelated text. If the input is a request, create a sensible original recipe that satisfies the user’s constraints (method, equipment, diet, cuisine, etc.) while staying realistic. Do not add explanations, commentary, or multiple recipes—only the single normalized recipe. If some information is missing and cannot be inferred reasonably, leave that field neutral/unknown instead of guessing wildly.",
    Input = "Chicken parm recipes",
    Model = ChatGPTModels.GPT5Mini,
    EnableWebSearch = true,
    ToolChoice = new ChatGPTToolChoice { Type = ChatGPTToolChoice.WebSearchPreview },
    Reasoning = new ChatGPTReasoningOptions { Effort = "medium", Summary = "auto" },
    Stream = stream // Set to true for streaming responses
};


try
{
    if (stream)
    {
        ChatGPTStreamActivity? lastActivity = null;
        await foreach (var evt in client.StreamEventsAsync(request))
        {
            if (rawEventWriter is not null)
            {
                var envelope = new
                {
                    timestamp = DateTimeOffset.UtcNow,
                    evt.RawEventName,
                    activity = evt.Activity.ToString(),
                    payload = evt.Payload
                };

                await rawEventWriter.WriteLineAsync(JsonSerializer.Serialize(envelope));
            }

            if (evt.Activity != lastActivity && evt.Activity != ChatGPTStreamActivity.StreamingText)
            {
                if (evt.IsWebSearch)
                {
                    Console.WriteLine($"\n[status] Running web search ({evt.ToolCallId ?? "unknown"})...");
                }
                else if (evt.IsThinking)
                {
                    Console.WriteLine($"\n[status] Thinking... ({evt.RawEventName})");
                }
                else if (evt.Activity == ChatGPTStreamActivity.ToolUse)
                {
                    Console.WriteLine($"\n[status] Tool call: {evt.ToolCallType ?? "unknown"} ({evt.RawEventName})");
                }
                else if (evt.Activity == ChatGPTStreamActivity.Completed)
                {
                    Console.WriteLine("\n[status] Completed");
                }
                else if (evt.Activity == ChatGPTStreamActivity.Error)
                {
                    Console.WriteLine($"\n[status] Error: {evt.RawEventName}");
                }
            }

            if (!string.IsNullOrEmpty(evt.TextDelta))
            {
                Console.Write(evt.TextDelta);
            }

            if (!string.IsNullOrEmpty(evt.ReasoningDelta))
            {
                Console.WriteLine($"\n[reasoning] {evt.ReasoningDelta}");
            }

            lastActivity = evt.Activity;
        }
    }
    else
    {
        LLMResponse<string> response = await client.QueryAsync<string>(request);
        Console.WriteLine(response.Result);
    }
}
finally
{
    if (rawEventWriter is not null)
    {
        await rawEventWriter.FlushAsync();
        rawEventWriter.Dispose();
        Console.WriteLine($"\n[debug] Event trace saved to {rawEventLogPath}");
    }
}


Console.ReadLine();
