namespace InstructSharp.Core;
public class LLMResponse<T>
{
    public string Id { get; set; }
    public string Model { get; set; }
    public T? Result { get; set; }
    public LLMUsage Usage { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
}