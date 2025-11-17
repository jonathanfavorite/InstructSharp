using System.Text.Json;

namespace InstructSharp.Clients.ChatGPT;

/// <summary>
/// Represents a single event emitted by the OpenAI Responses streaming API.
/// </summary>
public sealed class ChatGPTStreamEvent
{
    public string RawEventName { get; init; } = string.Empty;
    public ChatGPTStreamEventType EventType { get; init; } = ChatGPTStreamEventType.Unknown;
    public ChatGPTStreamActivity Activity { get; init; } = ChatGPTStreamActivity.Unknown;
    public string? TextDelta { get; init; }
    public string? ReasoningDelta { get; init; }
    public string? ToolCallType { get; init; }
    public string? ToolCallId { get; init; }
    public string? Status { get; init; }
    public bool IsThinking => Activity is ChatGPTStreamActivity.Initializing or ChatGPTStreamActivity.Thinking;
    public bool IsWebSearch => Activity == ChatGPTStreamActivity.WebSearch;
    public bool IsFinal => Activity is ChatGPTStreamActivity.Completed or ChatGPTStreamActivity.Error;
    public JsonElement Payload { get; init; } = default;
}

public enum ChatGPTStreamActivity
{
    Unknown,
    Initializing,
    Thinking,
    StreamingText,
    ToolUse,
    WebSearch,
    Completed,
    Error
}

public enum ChatGPTStreamEventType
{
    Unknown,
    ResponseCreated,
    ResponseInProgress,
    ResponseOutputTextDelta,
    ResponseOutputTextDone,
    ResponseOutputItemAdded,
    ResponseOutputItemDone,
    ResponseContentPartAdded,
    ResponseContentPartDone,
    ResponseReasoningDelta,
    ResponseReasoningDone,
    ResponseToolCallDelta,
    ResponseToolCallDone,
    ResponseCompleted,
    ResponseIncomplete,
    ResponseError,
    ResponseRefusalDelta,
    ResponseRefusalDone,
    LegacyChatCompletionsDelta
}
