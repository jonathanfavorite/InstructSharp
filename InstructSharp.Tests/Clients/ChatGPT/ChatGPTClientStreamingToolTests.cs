using System.Net;
using InstructSharp.Clients.ChatGPT;
using Xunit;

namespace InstructSharp.Tests.Clients.ChatGPT;

public sealed class ChatGPTClientStreamingToolTests
{
    [Fact]
    public async Task StreamEventsAsync_parses_function_call_output_item_done_with_call_metadata()
    {
        string arguments = """{"label":"Apply","url":"https://app.propertyware.com/pw/application/"}""";
        string stream =
            "event: response.output_item.done\n" +
            "data: {\"type\":\"response.output_item.done\",\"output_index\":0,\"item\":{\"id\":\"fc_123\",\"type\":\"function_call\",\"status\":\"completed\",\"name\":\"emit_cta\",\"call_id\":\"call_123\",\"arguments\":" +
            JsonString(arguments) +
            "}}\n\n" +
            "event: response.completed\n" +
            "data: {\"type\":\"response.completed\",\"response\":{\"status\":\"completed\"}}\n\n";
        ChatGPTClient client = new("test-key", new HttpClient(new SseHandler(stream))
        {
            BaseAddress = new Uri("https://api.openai.test/v1/")
        });

        List<ChatGPTStreamEvent> events = await CollectAsync(client.StreamEventsAsync(new ChatGPTRequest
        {
            Model = "gpt-5-mini",
            Input = "Apply",
            Stream = true
        }));

        ChatGPTStreamEvent toolEvent = Assert.Single(
            events,
            item => item.EventType == ChatGPTStreamEventType.ResponseOutputItemDone);
        Assert.Equal(ChatGPTStreamActivity.ToolUse, toolEvent.Activity);
        Assert.NotNull(toolEvent.ToolCall);
        Assert.Equal("function_call", toolEvent.ToolCall.Type);
        Assert.Equal("emit_cta", toolEvent.ToolCall.Name);
        Assert.Equal("call_123", toolEvent.ToolCall.CallId);
        Assert.Equal(arguments, toolEvent.ToolCall.ArgumentsJson);
        Assert.Null(toolEvent.TextDelta);
    }

    [Fact]
    public async Task StreamEventsAsync_does_not_expose_function_call_argument_deltas_as_text()
    {
        string stream = """
event: response.function_call_arguments.delta
data: {"type":"response.function_call_arguments.delta","item_id":"fc_123","output_index":0,"delta":"{\"label\":\"Apply\""}

""";
        ChatGPTClient client = new("test-key", new HttpClient(new SseHandler(stream))
        {
            BaseAddress = new Uri("https://api.openai.test/v1/")
        });

        ChatGPTStreamEvent streamEvent = Assert.Single(await CollectAsync(client.StreamEventsAsync(new ChatGPTRequest
        {
            Model = "gpt-5-mini",
            Input = "Apply",
            Stream = true
        })));

        Assert.Equal(ChatGPTStreamEventType.ResponseToolCallDelta, streamEvent.EventType);
        Assert.Equal(ChatGPTStreamActivity.ToolUse, streamEvent.Activity);
        Assert.NotNull(streamEvent.ToolCall);
        Assert.Equal("{\"label\":\"Apply\"", streamEvent.ToolCall.ArgumentsJson);
        Assert.Null(streamEvent.TextDelta);
    }

    private static string JsonString(string value) => System.Text.Json.JsonSerializer.Serialize(value);

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
    {
        List<T> items = new();
        await foreach (T item in source)
        {
            items.Add(item);
        }

        return items;
    }

    private sealed class SseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            });
    }
}
