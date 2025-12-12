using InstructSharp.Clients.ChatGPT;
using System.Text.Json;

namespace StreamingWithTools;

internal sealed class Program
{
    static async Task Main()
    {
        var poemSummaryTool = ChatGPTToolBuilder.Function("package_poem_metadata")
            .WithDescription("Collect metadata for the just-written poem, including title, tone, and number of lines.")
            .WithParameters<PoemSummaryArguments>()
            .Build();

        string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "API_KEY_HERE";

        ChatGPTClient client = new(apiKey);
        ChatGPTRequest request = new ChatGPTRequest
        {
            Model = ChatGPTModels.GPT4oMini,
            Instructions =
                "Follow this exact order:\n" +
                "  a) Stream a vivid summer poem about sunlight over the ocean as Markdown paragraphs.\n" +
                "  b) When (and only when) the poem text is fully streamed, invoke the package_poem_metadata tool with title, tone, line_count, and main_imagery.\n" +
                "Do not include metadata anywhere except in the tool call. The poem must appear before the tool call.",
            Input = "Write and analyze the poem per the policy above.",
            Stream = true,
            ToolChoice = new ChatGPTToolChoice { Type = "auto" }
        };
        request.AddTool(poemSummaryTool);

        Console.WriteLine("Streaming with tools... expecting a poem first, then a tool call.\n");

        List<string> poemSegments = new();
        bool poemCompleted = false;
        bool toolCallReceived = false;

        await foreach (var evt in client.StreamEventsAsync(request))
        {
            Console.WriteLine($"\n[event] {evt.EventType} ({evt.RawEventName}) | Activity={evt.Activity} | ToolId={evt.ToolCallId ?? "-"}");

            if (evt.EventType == ChatGPTStreamEventType.ResponseToolCallDelta &&
                evt.ToolCall is not null &&
                !string.IsNullOrEmpty(evt.ToolCall.ArgumentsJson))
            {
                Console.WriteLine($"[tool-args Δ] {evt.ToolCall.ArgumentsJson}");
            }
            else if (evt.EventType == ChatGPTStreamEventType.ResponseToolCallDone &&
                     evt.ToolCall is not null)
            {
                toolCallReceived = true;
                Console.WriteLine($"[tool-call] {evt.ToolCall.Name} ({evt.ToolCallId ?? evt.ToolCall.Id})");
                Console.WriteLine($"Arguments JSON: {evt.ToolCall.ArgumentsJson}");

                PoemSummaryArguments? parsed = evt.ToolCall.DeserializeArguments<PoemSummaryArguments>();
                if (parsed is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Parsed args -> title: {parsed.Title}, tone: {parsed.Tone}, lines: {parsed.LineCount}");
                    Console.ResetColor();
                }

                Console.WriteLine("\nTODO: POST your tool output back to OpenAI so the model can resume after processing the metadata.");
            }
            else if (!string.IsNullOrEmpty(evt.TextDelta))
            {
                poemSegments.Add(evt.TextDelta);
                Console.Write(evt.TextDelta);
            }
            else if (evt.EventType == ChatGPTStreamEventType.ResponseCompleted)
            {
                poemCompleted = true;
            }

            if (poemCompleted && toolCallReceived)
            {
                break;
            }
        }

        Console.WriteLine("\n---");
        Console.WriteLine("Final streamed poem text:");
        Console.WriteLine(string.Concat(poemSegments));
        Console.WriteLine(toolCallReceived
            ? "\nTool call captured above. Supply results via tool_outputs to complete the run."
            : "\nNo tool call observed—adjust your instructions if you need one.");
    }
}

internal sealed class PoemSummaryArguments
{
    public string Title { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public string MainImagery { get; set; } = string.Empty;
}
