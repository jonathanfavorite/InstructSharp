using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace InstructSharp.Clients.ChatGPT;
public class ChatGPTClient : BaseLLMClient<ChatGPTRequest>
{
    public ChatGPTClient(string apiKey, HttpClient? httpClient = null)
        : base(new HttpConfiguration
        {
            BaseUrl = "https://api.openai.com/v1/",
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

    public override LLMProvider GetLLMProvider() => LLMProvider.ChatGPT;
    protected override string GetEndpoint() => "responses";

    protected override object TransformRequest<T>(ChatGPTRequest request)
    {
        if(typeof(T) == typeof(string))
        {
            return new
            {
                model = request.Model,
                instructions = request.Instructions,
                input = request.Input,
                temperature = request.Temperature
            };
        }

        // using a custom object
        string customJsonSchema = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
        JsonNode schemaNode = JsonNode.Parse(customJsonSchema) ?? throw new InvalidCastException();

        return new
        {
            model = request.Model,
            instructions = request.Instructions,
            input = request.Input,
            temperature = request.Temperature,
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "mySchema",
                    schema = schemaNode
                }
            }
        };
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        ChatGPTResponse? casted = JsonSerializer.Deserialize<ChatGPTResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

        string raw = casted.output[0].content[0].text;

        LLMResponse<T> response = new LLMResponse<T>
        {
            Id = casted.id,
            Model = casted.model,
            Usage = new LLMUsage
            {
                PromptTokens = casted.usage?.input_tokens ?? 0,
                ResponseTokens = casted.usage?.output_tokens ?? 0,
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
