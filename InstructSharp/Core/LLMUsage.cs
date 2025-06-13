namespace InstructSharp.Core;
public class LLMUsage
{
    public int PromptTokens { get; set; }
    public int ResponseTokens { get; set; }
    public int TotalTokens { get; set; }
}
