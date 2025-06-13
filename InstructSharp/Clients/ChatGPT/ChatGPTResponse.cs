namespace InstructSharp.Clients.ChatGPT;

internal class ChatGPTResponse
{
    public string id { get; set; }
    public int created_at { get; set; }
    public string status { get; set; }
    public object error { get; set; }
    public object incomplete_details { get; set; }
    public object instructions { get; set; }
    public object max_output_tokens { get; set; }
    public string model { get; set; }
    public List<ChatGPTResponseOutput> output { get; set; }
    public bool parallel_tool_calls { get; set; }
    public object previous_response_id { get; set; }
    public ChatGPTResponseReasoning reasoning { get; set; }
    public bool store { get; set; }
    public double temperature { get; set; }
    public ChatGPTResponseText text { get; set; }
    public string tool_choice { get; set; }
    public List<object> tools { get; set; }
    public double top_p { get; set; }
    public string truncation { get; set; }
    public ChatGPTResponseUsage usage { get; set; }
    public object user { get; set; }
    public ChatGPTResponseMetadata metadata { get; set; }
}

internal class Content
{
    public string type { get; set; }
    public string text { get; set; }
    public List<object> annotations { get; set; }
}

internal class Format
{
    public string type { get; set; }
}

internal class ChatGPTResponseInputTokensDetails
{
    public int cached_tokens { get; set; }
}

internal class ChatGPTResponseMetadata
{
}

internal class ChatGPTResponseOutput
{
    public string type { get; set; }
    public string id { get; set; }
    public string status { get; set; }
    public string role { get; set; }
    public List<Content> content { get; set; }
}

internal class ChatGPTResponseOutputTokensDetails
{
    public int reasoning_tokens { get; set; }
}

internal class ChatGPTResponseReasoning
{
    public object effort { get; set; }
    public object summary { get; set; }
}



internal class ChatGPTResponseText
{
    public Format format { get; set; }
}

internal class ChatGPTResponseUsage
{
    public int input_tokens { get; set; }
    public ChatGPTResponseInputTokensDetails input_tokens_details { get; set; }
    public int output_tokens { get; set; }
    public ChatGPTResponseOutputTokensDetails output_tokens_details { get; set; }
    public int total_tokens { get; set; }
}