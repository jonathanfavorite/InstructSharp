using System.Text.Json;

namespace InstructSharp.Clients.ChatGPT;

/// <summary>
/// Represents a tool call emitted by ChatGPT (either from a streaming event or a synchronous response).
/// </summary>
public sealed class ChatGPTToolCall
{
    /// <summary>
    /// Key used when storing tool calls inside <see cref="Core.LLMResponse{T}.AdditionalData"/>.
    /// </summary>
    public const string AdditionalDataKey = "chatgpt:tool_calls";

    public string Id { get; init; } = string.Empty;
    public string? CallId { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? ArgumentsJson { get; init; }
    public string? Output { get; init; }
    public string? Status { get; init; }
    public string? RawItemId { get; init; }
    public int? OutputIndex { get; init; }

    public bool HasArguments => !string.IsNullOrWhiteSpace(ArgumentsJson);

    /// <summary>
    /// Deserialize the tool arguments into a strongly typed object.
    /// </summary>
    public T? DeserializeArguments<T>()
    {
        if (!HasArguments)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(ArgumentsJson!);
    }

    public override string ToString()
    {
        return $"{Type}:{Name ?? CallId ?? Id}";
    }
}
