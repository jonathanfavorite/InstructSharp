using InstructSharp.Clients.DeepSeek;
using InstructSharp.Clients.Grok;
using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Types;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.Grok;
public class GrokClient : BaseLLMClient<GrokRequest>
{
    public GrokClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            BaseUrl = "https://api.x.ai/v1/",
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

    public override LLMProvider GetLLMProvider() => LLMProvider.Grok;
    protected override string GetEndpoint() => "chat/completions";

    protected override object TransformRequest<T>(GrokRequest request)
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
        JsonNode schemaNode = JsonNode.Parse(customJsonSchema) ?? throw new InvalidCastException();

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
                },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "mySchema",
                    strict = true,
                    schema = schemaNode
                }
            }
        };
    }

    protected override object TransformRequestWithImages<T>(GrokRequest request)
    {
        throw new NotSupportedException("Image uploads are not currently supported.");
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        GrokResponse? casted = JsonSerializer.Deserialize<GrokResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

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
