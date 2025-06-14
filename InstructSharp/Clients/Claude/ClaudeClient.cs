using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Types;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.Claude;
public class ClaudeClient : BaseLLMClient<ClaudeRequest>
{
    public ClaudeClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            BaseUrl = "https://api.anthropic.com/v1/",
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
        _httpClient.DefaultRequestHeaders.Add("x-api-key", $"{_config.ApiKey}");
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", $"{ClaudeConstants.AnthripicVersion}");
    }

    public override LLMProvider GetLLMProvider() => LLMProvider.Claude;
    protected override string GetEndpoint() => "messages";

    protected override object TransformRequest<T>(ClaudeRequest request)
    {
        // base payload with top-level system prompt
        var basePayload = new
        {
            model = request.Model,
            temperature = request.Temperature,
            system = request.Instructions,
            // only user messages here
            messages = new[] { new { role = "user", content = request.Input } },
            max_tokens = request.MaxTokens
        };

        // if you need schema formatting, add it
        if (typeof(T) != typeof(string))
        {

            var combined = string.Join("\n\n", new[]
            {
                request.Instructions.Trim(),
                "Input:" + request.Input.Trim(),
                "Please invoke the responseSchema tool to emit JSON matching the schema."
            });

            var schemaJson = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
            var schemaNode = JsonNode.Parse(schemaJson);

            return new
            {
                model = request.Model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                tools = new[] {
                    new {
                        name        = "responseSchema",
                        description = "Return JSON matching the target schema.",
                        input_schema = schemaNode
                    }
                },
                tool_choice = new { type = "tool", name = "responseSchema" },
                messages = new[] {
                    new 
                    {
                        role = "user", 
                        content = combined
                    }
                }
            };
        }

        return basePayload;
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        var casted = JsonSerializer.Deserialize<ClaudeResponse>(jsonResponse, _jsonOptions)
                     ?? throw new InvalidOperationException("Empty response");

        var response = new LLMResponse<T>
        {
            Id = casted.id,
            Model = casted.model,
            Usage = new LLMUsage
            {
                PromptTokens = casted.usage?.input_tokens ?? 0,
                ResponseTokens = casted.usage?.output_tokens ?? 0,
                TotalTokens = (casted.usage?.input_tokens + casted.usage?.output_tokens) ?? 0
            }
        };

        // Plain‐string case
        if (typeof(T) == typeof(string))
        {
            var rawText = casted.content[0].text ?? "";
            response.Result = (T)(object)rawText;
            return response;
        }

        // Structured JSON via tool_use
        var block = casted.content[0];
        if (block.type == "tool_use")
        {
            string json = block.input.GetRawText();
            response.Result = JsonSerializer.Deserialize<T>(json)
                         ?? throw new InvalidOperationException($"Couldn't parse JSON into {typeof(T).Name}");

            return response;
        }

        var fallback = casted.content[0].text ?? "";
        response.Result = JsonSerializer.Deserialize<T>(fallback)
                          ?? throw new InvalidOperationException($"Fallback JSON parse failed for {typeof(T).Name}");
        return response;
    }
}
