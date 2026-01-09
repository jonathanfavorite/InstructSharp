using InstructSharp.Core;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace InstructSharp.Clients;
public abstract class BaseLLMClient<TRequest> : ILLMClient where TRequest: class, ILLMRequest, new()
{
    protected readonly HttpClient _httpClient;
    protected readonly HttpConfiguration _config;
    protected readonly JsonSerializerOptions _jsonOptions;
    protected readonly bool _httpClientWasProvided;

    protected BaseLLMClient(HttpConfiguration config, HttpClient? httpClient = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClientWasProvided = httpClient is not null;
        _httpClient = httpClient ?? new HttpClient();

        // Only set base address if we're not using URL patterns or delayed setup
        if (!_config.SetupBaseUrlAfterConstructor && string.IsNullOrEmpty(_config.UrlPattern))
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

    public abstract LLMProvider GetLLMProvider();
    protected abstract void ConfigureHttpClient();
    protected abstract object TransformRequest<T>(TRequest request);
    protected abstract object TransformRequestWithImages<T>(TRequest request);
    protected abstract LLMResponse<T> TransformResponse<T>(string jsonResponse);
    protected abstract string GetEndpoint();

    protected virtual string BuildRequestUrl(string model)
    {
        if (!string.IsNullOrEmpty(_config.UrlPattern))
        {
            // Replace {model} placeholder in URL pattern
            return _config.UrlPattern.Replace("{model}", model);
        }

        // Default behavior - use base URL + endpoint
        return GetEndpoint();
    }

    public Task<LLMResponse<T>> QueryAsync<T>(string instructions, string? input = null)
    {
        var req = new TRequest
        {
            Model = _config.Model,
            Instructions = instructions,
            Input = input ?? ""
        };
        return QueryAsync<T>(req);
    }

    public async Task<LLMResponse<T>> QueryAsync<T>(ILLMRequest request)
    {
        if (request is not TRequest typed)
            throw new InvalidCastException(
                $"Expected a {typeof(TRequest).Name}, got {request.GetType().Name}");
        return await QueryAsync<T>(typed);
    }

    public virtual async Task<LLMResponse<T>> QueryAsync<T>(TRequest request)
    {
        object providerRequest;
        if(request.ContainsImages)
        {
            providerRequest = TransformRequestWithImages<T>(request);
        }
        else
        {
            providerRequest = TransformRequest<T>(request);
        }
        
        var json = JsonSerializer.Serialize(providerRequest, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Build the request URL with the model
        var requestUrl = BuildRequestUrl(request.Model);

        HttpResponseMessage response;
        if (!string.IsNullOrEmpty(_config.UrlPattern))
        {
            // For URL patterns, use absolute URL
            response = await _httpClient.PostAsync(requestUrl, content);
        }
        else
        {
            // For standard base URL + endpoint
            response = await _httpClient.PostAsync(requestUrl, content);
        }

        if (!response.IsSuccessStatusCode)
        {
            // for debugging the error
            //string error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}. " +
                                            $"Response: {await response.Content.ReadAsStringAsync()}");
        }
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        return TransformResponse<T>(responseJson);
    }

    public virtual HttpRequestMessage BuildStreamingRequest<T>(TRequest request)
    {
        throw new NotSupportedException($"Streaming is not supported for {GetType().Name}.");
    }

    public virtual string ParseStreamedChunk<T>(string payload)
    {
        throw new NotSupportedException($"Streaming is not supported for {GetType().Name}.");
    }

    public virtual async IAsyncEnumerable<string> StreamQueryAsync<T>(TRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = BuildStreamingRequest<T>(request);
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        //Console.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line == null || !line.StartsWith("data: ")) continue;
            var payload = line["data: ".Length..].Trim();
            if (payload == "[DONE]") break;
            var chunk = ParseStreamedChunk<T>(payload);
            if (!string.IsNullOrEmpty(chunk))
                yield return chunk;
        }
    }
}
