using InstructSharp.Core;

namespace InstructSharp.Interfaces;
public interface ILLMClient
{
    Task<LLMResponse<T>> QueryAsync<T>(string instructions, string? input = null);
}
