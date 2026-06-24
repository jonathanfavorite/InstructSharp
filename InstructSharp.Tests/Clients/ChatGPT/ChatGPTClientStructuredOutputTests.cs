using System.Text.Json;
using System.Text.Json.Nodes;
using InstructSharp.Clients.ChatGPT;
using Xunit;

namespace InstructSharp.Tests.Clients.ChatGPT;

public class ChatGPTClientStructuredOutputTests
{
    [Fact]
    public void TransformRequest_WhenResponseTypeIsStructured_EnablesStrictJsonSchema()
    {
        TestChatGPTClient client = new();

        object payload = client.BuildRequest<StructuredResult>(new ChatGPTRequest
        {
            Model = "gpt-5-nano",
            Instructions = "Return structured JSON.",
            Input = "hello"
        });

        JsonNode root = JsonNode.Parse(JsonSerializer.Serialize(payload))!;

        Assert.True(root["text"]?["format"]?["strict"]?.GetValue<bool>());
    }

    [Fact]
    public void TransformRequest_WhenInputItemsAreProvided_SerializesResponsesInputArray()
    {
        TestChatGPTClient client = new();

        object payload = client.BuildRequest<string>(new ChatGPTRequest
        {
            Model = "gpt-5-mini",
            Input = "ignored when input items are present",
            InputItems =
            [
                ChatGPTInputItem.FunctionCallOutput("call_123", "CTA emitted to the client."),
                ChatGPTInputItem.UserText("tell me about the process")
            ]
        });

        JsonNode root = JsonNode.Parse(JsonSerializer.Serialize(payload))!;
        JsonArray input = Assert.IsType<JsonArray>(root["input"]);

        Assert.Equal("function_call_output", input[0]?["type"]?.GetValue<string>());
        Assert.Equal("call_123", input[0]?["call_id"]?.GetValue<string>());
        Assert.Equal("CTA emitted to the client.", input[0]?["output"]?.GetValue<string>());
        Assert.Equal("completed", input[0]?["status"]?.GetValue<string>());
        Assert.Equal("message", input[1]?["type"]?.GetValue<string>());
        Assert.Equal("user", input[1]?["role"]?.GetValue<string>());
        Assert.Equal("tell me about the process", input[1]?["content"]?.GetValue<string>());
    }

    [Fact]
    public void TransformResponse_WhenStructuredOutputIsIncomplete_ThrowsActionableException()
    {
        TestChatGPTClient client = new();
        string responseJson = """
{
  "id": "resp_test",
  "model": "gpt-5-nano",
  "status": "incomplete",
  "incomplete_details": { "reason": "max_output_tokens" },
  "output": [
    {
      "type": "message",
      "status": "incomplete",
      "content": [
        {
          "type": "output_text",
          "text": "{\"items\":[{\"value\":\"unfinished"
        }
      ]
    }
  ],
  "usage": { "input_tokens": 1, "output_tokens": 2, "total_tokens": 3 }
}
""";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => client.ParseResponse<StructuredResult>(responseJson));

        Assert.Contains("incomplete", exception.Message);
        Assert.Contains("max_output_tokens", exception.Message);
        Assert.Contains("resp_test", exception.Message);
    }

    [Fact]
    public void TransformResponse_WhenStructuredJsonIsMalformed_ThrowsActionableExceptionWithRawPreview()
    {
        TestChatGPTClient client = new();
        string responseJson = """
{
  "id": "resp_test",
  "model": "gpt-5-nano",
  "status": "completed",
  "output_text": "{\"items\":[{\"value\":\"unfinished",
  "usage": { "input_tokens": 1, "output_tokens": 2, "total_tokens": 3 }
}
""";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => client.ParseResponse<StructuredResult>(responseJson));

        Assert.Contains("Failed to deserialize", exception.Message);
        Assert.Contains("resp_test", exception.Message);
        Assert.Contains("{\"items\"", exception.Message);
    }

    private sealed class TestChatGPTClient : ChatGPTClient
    {
        public TestChatGPTClient()
            : base("test-key")
        {
        }

        public object BuildRequest<T>(ChatGPTRequest request)
        {
            return TransformRequest<T>(request);
        }

        public T? ParseResponse<T>(string jsonResponse)
        {
            return TransformResponse<T>(jsonResponse).Result;
        }
    }

    private sealed class StructuredResult
    {
        public List<StructuredItem> Items { get; set; } = [];
    }

    private sealed class StructuredItem
    {
        public string Value { get; set; } = string.Empty;
    }
}
