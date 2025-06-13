using InstructSharp.Core;
using InstructSharp.Types;

namespace InstructSharp.Interfaces;
public interface ILLMClient
{
    LLMProvider GetLLMProvider();
    Task<LLMResponse<T>> QueryAsync<T>(ILLMRequest request);
    Task<LLMResponse<T>> QueryAsync<T>(string instructions, string? input = null);
}
