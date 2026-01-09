using InstructSharp.Clients.Claude;
using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Types;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.DeepSeek;
public class DeepSeekClient : BaseLLMClient<DeepSeekRequest>
{
    public DeepSeekClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            BaseUrl = "https://api.deepseek.com/",
            ApiKey = apiKey,
            DefaultHeaders = new Dictionary<string, string>
            {
                ["Accept"] = "application/json",
                ["User-Agent"] = "LLMClients/1.0"
            }
        }, httpClient)
    { }

    protected override void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        // Only set timeout if no HttpClient was provided (respect user's configured timeout)
        if (!_httpClientWasProvided)
        {
            _httpClient.Timeout = _config.Timeout;
        }
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    public override LLMProvider GetLLMProvider() => LLMProvider.DeepSeek;
    protected override string GetEndpoint() => "chat/completions";

    protected override object TransformRequest<T>(DeepSeekRequest request)
    {
        if (typeof(T) == typeof(string))
        {
            return new
            {
                model = request.Model,
                messages = new[] {
                    new {
                        role    = "system",
                        content = request.Instructions
                    },
                    new {
                        role    = "user",
                        content = request.Input
                    }
                },
                temperature = request.Temperature
            };
        }

        string schemaExample = LLMSchemaHelper.GenerateJsonSchema(typeof(T));

        return new
        {
            model = request.Model,
            messages = new[] {
            new {
                role    = "system",
                content = $"{request.Instructions} !!!Important: Respond ONLY with JSON matching this schema:\n\n{schemaExample}"
            },
            new {
                role    = "user",
                content = request.Input
            }
        },
            temperature = request.Temperature,
            response_format = new
            {
                type = "json_object"
            }
        };
    }

    protected override object TransformRequestWithImages<T>(DeepSeekRequest request)
    {
        throw new NotSupportedException("Image uploads are not currently supported.");
    }


    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        DeepSeekResponse? casted = JsonSerializer.Deserialize<DeepSeekResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

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
