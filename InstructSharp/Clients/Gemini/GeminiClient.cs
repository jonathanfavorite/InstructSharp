using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Types;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.Gemini;
public class GeminiClient : BaseLLMClient<GeminiRequest>
{
    public GeminiClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            UrlPattern = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key=" + apiKey,
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
        _httpClient.Timeout = _config.Timeout;
    }

    public override LLMProvider GetLLMProvider() => LLMProvider.Gemini;
    protected override string GetEndpoint() => "";

    protected override object TransformRequest<T>(GeminiRequest request)
    {
        if (typeof(T) == typeof(string))
        {
            return new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new { text = request.Instructions }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = request.Instructions }
                        }
                    }
                }
            };
        }


        // using a custom object
        string customJsonSchema = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
        var schemaElement = JsonSerializer.Deserialize<JsonElement>(customJsonSchema);

        return new
        {
            system_instruction = new
            {
                parts = new[]
                    {
                        new { text = request.Instructions }
                    }
            },
            contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = request.Input }
                        }
                    }
                },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseJsonSchema = schemaElement
            }
        };
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        GeminiResponse? casted = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

        string raw = casted.candidates[0].content.parts[0].text;

        LLMResponse<T> response = new LLMResponse<T>
        {
            Id = casted.responseId,
            Model = casted.modelVersion,
            Usage = new LLMUsage
            {
                PromptTokens = casted.usageMetadata?.promptTokenCount ?? 0,
                ResponseTokens = casted.usageMetadata?.candidatesTokenCount ?? 0,
                TotalTokens = casted.usageMetadata?.promptTokenCount ?? 0
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
