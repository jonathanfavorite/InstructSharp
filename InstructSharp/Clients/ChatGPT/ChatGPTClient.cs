﻿using InstructSharp.Core;
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

    protected override object TransformRequestWithImages<T>(ChatGPTRequest request)
    {

        Dictionary<int, string> qualityDict = new Dictionary<int, string>
        {
            { 0, "auto" },
            { 1, "low" },
            { 2, "high" },
            { 3, "high" }
        };

        // 1) System message content
        var systemContent = new[]
        {
        new {
            type = "input_text",
            text = request.Instructions ?? ""
        }
    };

        // 2) User message content items
        var userContentItems = new List<object>();
        if (!string.IsNullOrEmpty(request.Input))
        {
            userContentItems.Add(new
            {
                type = "input_text",
                text = request.Input
            });
        }
        foreach (var img in request.Images)
        {
            userContentItems.Add(new
            {
                type = "input_image",
                image_url = img.Url,    // HTTP URL or base64 data-URI
                detail = qualityDict[img.DetailRequired]
            });
        }

        // 3) Wrap into the top-level input array
        object[] input = new object[]
        {
        new { role = "system", content = systemContent },
        new { role = "user",   content = userContentItems.ToArray() }
        };

        // 4) Base payload with model, input, temperature
        var payload = new Dictionary<string, object>
        {
            ["model"] = request.Model,
            ["input"] = input,
            ["temperature"] = request.Temperature
        };

        // 5) **Structured output**: under text.format
        if (typeof(T) != typeof(string))
        {
            // Generate your JSON schema object
            string schemaJson = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
            var schemaNode = JsonNode.Parse(schemaJson)!;

            payload["text"] = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "mySchema",
                    schema = schemaNode
                }
            };
        }

        return payload;
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
