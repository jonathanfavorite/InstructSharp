using InstructSharp.Clients.ChatGPT;
using InstructSharp.Clients.LLama;
using InstructSharp.Core;
using InstructSharp.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.LLama;
public class LLamaClient : BaseLLMClient<LLamaRequest>
{
    public LLamaClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            BaseUrl = "https://api.deepinfra.com/v1/",
            ApiKey = apiKey,
            DefaultHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["User-Agent"] = Constants.UserAgentHeader
            }
        }, httpClient)
    { }

    protected override void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.Timeout = _config.Timeout;
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    protected override string GetEndpoint() => "openai/chat/completions";

    protected override object TransformRequest<T>(LLamaRequest request)
    {
        if (typeof(T) == typeof(string))
        {
            return new
            {
                model = request.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = request.Instructions
                    },
                    new
                    {
                        role = "user",
                        content = request.Input
                    }
                }
            };
        }

        // using a custom object
        string customJsonSchema = LLMSchemaHelper.GenerateJsonSchema(typeof(T));

        return new
        {
            model = request.Model,
            messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $"{request.Instructions} !!!Important: Respond ONLY with JSON matching this schema:\n\n{customJsonSchema}"
                    },
                    new
                    {
                        role = "user",
                        content = request.Input
                    }
                },
            response_format = new
            {
                type = "json_object"
            }
        };
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        LLamaResponse? casted = JsonSerializer.Deserialize<LLamaResponse>(jsonResponse, _jsonOptions) 
            ?? throw new InvalidOperationException("Empty response");

        string raw = casted.choices[0].message.content;

        LLMResponse<T> response = new LLMResponse<T>
        {
            Id = casted.id,
            Model = casted.model,
            Usage = new LLMUsage
            {
                PromptTokens = casted.usage?.prompt_tokens ?? 0,
                ResponseTokens = casted.usage?.completion_tokens ?? 0,
                TotalTokens = casted.usage?.total_tokens ?? 0
            }
        };

        if (typeof(T) == typeof(string))
        {
            response.Result = (T)(object)raw;
            return response;
        }

        response.Result = JsonSerializer.Deserialize<T>(raw)
            ?? throw new InvalidOperationException($"Failed to deserialize response into type {typeof(T).Name}");
        return response;
    }

}
