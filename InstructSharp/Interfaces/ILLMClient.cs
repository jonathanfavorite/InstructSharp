using InstructSharp.Core;

namespace InstructSharp.Interfaces;
public interface ILLMClient<TLLMRequest>
{
    Task<LLMResponse<T>> QueryAsync<T>(TLLMRequest request);
}
