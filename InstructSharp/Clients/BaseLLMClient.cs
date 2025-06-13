using InstructSharp.Core;
using InstructSharp.Interfaces;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace InstructSharp.Clients;
public abstract class BaseLLMClient<TRequest> : ILLMClient where TRequest: class, ILLMRequest, new()
{
    protected readonly HttpClient _httpClient;
    protected readonly HttpConfiguration _config;
    protected readonly JsonSerializerOptions _jsonOptions;

    protected BaseLLMClient(HttpConfiguration config, HttpClient? httpClient = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        _httpClient = httpClient ?? new HttpClient();
        if(!_config.SetupBaseUrlAfterConstructor)
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        }

        // Default headers
        foreach (var header in _config.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        ConfigureHttpClient();
    }

    protected abstract void ConfigureHttpClient();
    protected abstract object TransformRequest<T>(TRequest request);
    protected abstract LLMResponse<T> TransformResponse<T>(string jsonResponse);
    protected abstract string GetEndpoint();

    public Task<LLMResponse<T>> QueryAsync<T>(string instructions, string? input = null)
    {
        var req = new TRequest
        {
            Model = _config.Model,
            Instructions = instructions,
            Input = input
        };
        return QueryAsync<T>(req);
    }

public virtual async Task<LLMResponse<T>> QueryAsync<T>(TRequest request)
    {
        var providerRequest = TransformRequest<T>(request);
        var json = JsonSerializer.Serialize(providerRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(GetEndpoint(), content);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
        }
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return TransformResponse<T>(responseJson);
    }
}
