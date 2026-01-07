using System;
using System.Collections.Generic;

namespace InstructSharp.Clients.ChatGPT;

public class ChatGPTConversationCreateRequest
{
    public Dictionary<string, string>? Metadata { get; set; }
    public List<object>? Items { get; set; }
}

public class ChatGPTConversation
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

internal class ChatGPTConversationResponse
{
    public string id { get; set; }
    public int created_at { get; set; }
    public Dictionary<string, string>? metadata { get; set; }
}
