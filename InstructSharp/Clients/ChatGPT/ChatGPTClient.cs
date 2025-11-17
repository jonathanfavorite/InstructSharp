using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["instructions"] = request.Instructions,
            ["input"] = request.Input,
            ["stream"] = request.Stream
        };

        ApplyCommonRequestOptions(payload, request);

        var tools = BuildToolsPayload(request);
        if (tools.Count > 0)
        {
            payload["tools"] = tools;
        }

        if (typeof(T) != typeof(string))
        {
            // using a custom object
            string customJsonSchema = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
            JsonNode schemaNode = JsonNode.Parse(customJsonSchema) ?? throw new InvalidCastException();

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
        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["input"] = input
        };

        ApplyCommonRequestOptions(payload, request);

        var tools = BuildToolsPayload(request);
        if (tools.Count > 0)
        {
            payload["tools"] = tools;
        }

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

    private static void ApplyCommonRequestOptions(Dictionary<string, object?> payload, ChatGPTRequest request)
    {
        var reasoningPayload = BuildReasoningPayload(request);
        if (reasoningPayload is not null)
        {
            payload["reasoning"] = reasoningPayload;
        }

        if (request.Include.Count > 0)
        {
            payload["include"] = request.Include;
        }

        var toolChoice = BuildToolChoicePayload(request.ToolChoice);
        if (toolChoice is not null)
        {
            payload["tool_choice"] = toolChoice;
        }
    }

    private static object? BuildReasoningPayload(ChatGPTRequest request)
    {
        if (request.Reasoning is null)
        {
            return null;
        }

        Dictionary<string, object?> map = new();
        if (!string.IsNullOrWhiteSpace(request.Reasoning.Effort))
        {
            map["effort"] = request.Reasoning.Effort;
        }

        if (!string.IsNullOrWhiteSpace(request.Reasoning.Summary))
        {
            map["summary"] = request.Reasoning.Summary;
        }

        return map.Count == 0 ? null : map;
    }

    private static object? BuildToolChoicePayload(ChatGPTToolChoice? toolChoice)
    {
        if (toolChoice is null || string.IsNullOrWhiteSpace(toolChoice.Type))
        {
            return null;
        }

        string normalized = toolChoice.Type.Trim();
        if (string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.ToLowerInvariant();
        }

        var payload = new Dictionary<string, object?>
        {
            ["type"] = normalized
        };

        if (!string.IsNullOrWhiteSpace(toolChoice.FunctionName))
        {
            payload["function"] = new { name = toolChoice.FunctionName };
        }

        return payload;
    }

    private static List<object> BuildToolsPayload(ChatGPTRequest request)
    {
        List<object> tools = new();

        if (request.EnableWebSearch)
        {
            tools.Add(BuildWebSearchTool(request));
        }

        if (request.EnableFileSearch)
        {
            tools.Add(new Dictionary<string, object?>
            {
                ["type"] = "file_search"
            });
        }

        if (request.EnableImageGeneration)
        {
            tools.Add(new Dictionary<string, object?>
            {
                ["type"] = "image_generation"
            });
        }

        if (request.EnableCodeInterpreter)
        {
            tools.Add(new Dictionary<string, object?>
            {
                ["type"] = "code_interpreter"
            });
        }

        if (request.EnableComputerUse)
        {
            tools.Add(new Dictionary<string, object?>
            {
                ["type"] = "computer-use-preview"
            });
        }

        foreach (ChatGPTToolSpecification tool in request.CustomTools)
        {
            if (string.IsNullOrWhiteSpace(tool.Type))
            {
                continue;
            }

            Dictionary<string, object?> expanded = new(tool.Parameters)
            {
                ["type"] = tool.Type
            };
            tools.Add(expanded);
        }

        return tools;
    }

    private static Dictionary<string, object?> BuildWebSearchTool(ChatGPTRequest request)
    {
        var userLocation = new Dictionary<string, object?>
        {
            ["type"] = "approximate",
            ["city"] = null,
            ["country"] = request.WebSearchUserCountry,
            ["region"] = null,
            ["timezone"] = null
        };

        return new Dictionary<string, object?>
        {
            ["type"] = request.WebSearchToolType,
            ["filters"] = null,
            ["search_context_size"] = request.WebSearchContextSize,
            ["user_location"] = userLocation
        };
    }

    public override HttpRequestMessage BuildStreamingRequest<T>(ChatGPTRequest request)
    {
        request.Stream = true;
        object providerRequest = request.ContainsImages
            ? TransformRequestWithImages<T>(request)
            : TransformRequest<T>(request);

        var json = JsonSerializer.Serialize(providerRequest, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var requestUrl = BuildRequestUrl(request.Model);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = content
        };

        httpRequest.Headers.Accept.Clear();
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        foreach (var header in _httpClient.DefaultRequestHeaders)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        return httpRequest;
    }

    public override async IAsyncEnumerable<string> StreamQueryAsync<T>(ChatGPTRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var streamEvent in StreamEventsAsync(request, cancellationToken))
        {
            if (!string.IsNullOrEmpty(streamEvent.TextDelta))
            {
                yield return streamEvent.TextDelta!;
            }
        }
    }

    public async IAsyncEnumerable<ChatGPTStreamEvent> StreamEventsAsync(ChatGPTRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var httpRequest = BuildStreamingRequest<string>(request);
        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEventName = null;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rawLine = await reader.ReadLineAsync();
            if (rawLine is null)
            {
                continue;
            }

            if (rawLine.StartsWith(":", StringComparison.Ordinal))
            {
                continue; // comment / heartbeat
            }

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                currentEventName = null;
                continue;
            }

            if (rawLine.StartsWith("event:", StringComparison.Ordinal))
            {
                currentEventName = rawLine["event:".Length..].Trim();
                continue;
            }

            if (!rawLine.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = rawLine["data:".Length..].Trim();
            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                yield break;
            }

            var parsedEvent = TryParseStreamEvent(currentEventName, payload);
            if (parsedEvent is not null)
            {
                yield return parsedEvent;
            }
        }
    }

    private static ChatGPTStreamEvent? TryParseStreamEvent(string? eventName, string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var resolvedEventName = !string.IsNullOrWhiteSpace(eventName)
                ? eventName
                : root.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;

            if (string.IsNullOrWhiteSpace(resolvedEventName) && root.TryGetProperty("choices", out var choices))
            {
                var legacyDelta = ExtractLegacyCompletionsDelta(choices);
                if (!string.IsNullOrEmpty(legacyDelta))
                {
                    return new ChatGPTStreamEvent
                    {
                        RawEventName = "chat.completions.delta",
                        EventType = ChatGPTStreamEventType.LegacyChatCompletionsDelta,
                        Activity = ChatGPTStreamActivity.StreamingText,
                        TextDelta = legacyDelta,
                        Payload = root.Clone()
                    };
                }
            }

            resolvedEventName ??= string.Empty;
            var payloadClone = root.Clone();

            var eventType = ResolveEventType(resolvedEventName);
            var status = ExtractStatus(root);
            var (toolType, toolId) = ExtractToolInfo(root);
            var textDelta = ExtractTextDelta(root);
            var reasoningDelta = TryGetReasoningDelta(root);
            var activity = ResolveActivity(eventType, toolType, status);

            return new ChatGPTStreamEvent
            {
                RawEventName = resolvedEventName,
                EventType = eventType,
                Activity = activity,
                TextDelta = textDelta,
                ReasoningDelta = reasoningDelta,
                ToolCallType = toolType,
                ToolCallId = toolId,
                Status = status,
                Payload = payloadClone
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractLegacyCompletionsDelta(JsonElement choices)
    {
        if (choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
        {
            return null;
        }

        var firstChoice = choices[0];
        if (!firstChoice.TryGetProperty("delta", out var delta))
        {
            return null;
        }

        if (delta.TryGetProperty("content", out var contentElem))
        {
            if (contentElem.ValueKind == JsonValueKind.String)
            {
                return contentElem.GetString();
            }

            if (contentElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in contentElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("text", out var textProp) &&
                        textProp.ValueKind == JsonValueKind.String)
                    {
                        var value = textProp.GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string? ExtractStatus(JsonElement root)
    {
        if (root.TryGetProperty("response", out var responseElem) &&
            responseElem.ValueKind == JsonValueKind.Object &&
            responseElem.TryGetProperty("status", out var statusProp) &&
            statusProp.ValueKind == JsonValueKind.String)
        {
            return statusProp.GetString();
        }

        return null;
    }

    private static (string? ToolType, string? ToolId) ExtractToolInfo(JsonElement root)
    {
        if (!root.TryGetProperty("delta", out var delta) || delta.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var toolCall in toolCalls.EnumerateArray())
            {
                var toolType = toolCall.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
                    ? typeProp.GetString()
                    : null;
                var toolId = toolCall.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                    ? idProp.GetString()
                    : null;

                if (!string.IsNullOrEmpty(toolType))
                {
                    return (toolType, toolId);
                }
            }
        }

        if (delta.TryGetProperty("web_search_call", out var webSearchCall) && webSearchCall.ValueKind == JsonValueKind.Object)
        {
            var callId = webSearchCall.TryGetProperty("call_id", out var callIdProp) && callIdProp.ValueKind == JsonValueKind.String
                ? callIdProp.GetString()
                : null;
            return ("web_search", callId);
        }

        return (null, null);
    }

    private static ChatGPTStreamEventType ResolveEventType(string? eventName)
    {
        return eventName switch
        {
            "response.created" => ChatGPTStreamEventType.ResponseCreated,
            "response.in_progress" => ChatGPTStreamEventType.ResponseInProgress,
            "response.output_text.delta" => ChatGPTStreamEventType.ResponseOutputTextDelta,
            "response.output_text.done" => ChatGPTStreamEventType.ResponseOutputTextDone,
            "response.output_item.added" => ChatGPTStreamEventType.ResponseOutputItemAdded,
            "response.output_item.done" => ChatGPTStreamEventType.ResponseOutputItemDone,
            "response.content_part.added" => ChatGPTStreamEventType.ResponseContentPartAdded,
            "response.content_part.done" => ChatGPTStreamEventType.ResponseContentPartDone,
            "response.reasoning.delta" => ChatGPTStreamEventType.ResponseReasoningDelta,
            "response.reasoning.done" => ChatGPTStreamEventType.ResponseReasoningDone,
            "response.tool_call.delta" => ChatGPTStreamEventType.ResponseToolCallDelta,
            "response.tool_call.done" => ChatGPTStreamEventType.ResponseToolCallDone,
            "response.completed" => ChatGPTStreamEventType.ResponseCompleted,
            "response.incomplete" => ChatGPTStreamEventType.ResponseIncomplete,
            "response.error" => ChatGPTStreamEventType.ResponseError,
            "response.refusal.delta" => ChatGPTStreamEventType.ResponseRefusalDelta,
            "response.refusal.done" => ChatGPTStreamEventType.ResponseRefusalDone,
            _ when string.Equals(eventName, "chat.completions.delta", StringComparison.OrdinalIgnoreCase) => ChatGPTStreamEventType.LegacyChatCompletionsDelta,
            _ => ChatGPTStreamEventType.Unknown
        };
    }

    private static ChatGPTStreamActivity ResolveActivity(ChatGPTStreamEventType eventType, string? toolType, string? status)
    {
        return eventType switch
        {
            ChatGPTStreamEventType.ResponseCreated => ChatGPTStreamActivity.Initializing,
            ChatGPTStreamEventType.ResponseInProgress => ChatGPTStreamActivity.Thinking,
            ChatGPTStreamEventType.ResponseReasoningDelta => ChatGPTStreamActivity.Thinking,
            ChatGPTStreamEventType.ResponseReasoningDone => ChatGPTStreamActivity.Thinking,
            ChatGPTStreamEventType.ResponseOutputTextDelta => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseOutputTextDone => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseOutputItemAdded => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseOutputItemDone => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseContentPartAdded => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseContentPartDone => ChatGPTStreamActivity.StreamingText,
            ChatGPTStreamEventType.ResponseToolCallDelta => string.Equals(toolType, "web_search", StringComparison.OrdinalIgnoreCase)
                ? ChatGPTStreamActivity.WebSearch
                : ChatGPTStreamActivity.ToolUse,
            ChatGPTStreamEventType.ResponseToolCallDone => string.Equals(toolType, "web_search", StringComparison.OrdinalIgnoreCase)
                ? ChatGPTStreamActivity.WebSearch
                : ChatGPTStreamActivity.ToolUse,
            ChatGPTStreamEventType.ResponseCompleted => ChatGPTStreamActivity.Completed,
            ChatGPTStreamEventType.ResponseIncomplete => ChatGPTStreamActivity.Error,
            ChatGPTStreamEventType.ResponseError => ChatGPTStreamActivity.Error,
            ChatGPTStreamEventType.ResponseRefusalDelta => ChatGPTStreamActivity.Error,
            ChatGPTStreamEventType.ResponseRefusalDone => ChatGPTStreamActivity.Error,
            ChatGPTStreamEventType.LegacyChatCompletionsDelta => ChatGPTStreamActivity.StreamingText,
            _ => ChatGPTStreamActivity.Thinking
        };
    }

    private static string? TryGetReasoningDelta(JsonElement root)
    {
        if (!root.TryGetProperty("delta", out var delta))
        {
            return TryGetTopLevelString(root, "reasoning");
        }

        if (delta.ValueKind == JsonValueKind.String)
        {
            return delta.GetString();
        }

        if (delta.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (delta.TryGetProperty("reasoning", out var reasoningElem))
        {
            if (reasoningElem.ValueKind == JsonValueKind.String)
            {
                return reasoningElem.GetString();
            }

            if (reasoningElem.ValueKind == JsonValueKind.Object &&
                reasoningElem.TryGetProperty("text", out var reasoningText) &&
                reasoningText.ValueKind == JsonValueKind.String)
            {
                return reasoningText.GetString();
            }
        }

        if (delta.TryGetProperty("reasoning_output_text", out var reasoningArray) && reasoningArray.ValueKind == JsonValueKind.Array)
        {
            StringBuilder builder = new();
            foreach (var node in reasoningArray.EnumerateArray())
            {
                if (node.ValueKind == JsonValueKind.String)
                {
                    builder.Append(node.GetString());
                    continue;
                }

                if (node.ValueKind == JsonValueKind.Object &&
                    node.TryGetProperty("text", out var nodeText) &&
                    nodeText.ValueKind == JsonValueKind.String)
                {
                    builder.Append(nodeText.GetString());
                }
            }

            if (builder.Length > 0)
            {
                return builder.ToString();
            }
        }

        return null;
    }

    private static string? ExtractTextDelta(JsonElement root)
    {
        if (!root.TryGetProperty("delta", out var delta))
        {
            return null;
        }

        switch (delta.ValueKind)
        {
            case JsonValueKind.String:
                return delta.GetString();

            case JsonValueKind.Object:
                if (delta.TryGetProperty("text", out var textProp) && textProp.ValueKind == JsonValueKind.String)
                {
                    return textProp.GetString();
                }

                if (delta.TryGetProperty("content", out var contentProp) && contentProp.ValueKind == JsonValueKind.String)
                {
                    return contentProp.GetString();
                }

                if (delta.TryGetProperty("output_text", out var outputTextProp) && outputTextProp.ValueKind == JsonValueKind.String)
                {
                    return outputTextProp.GetString();
                }
                break;

            case JsonValueKind.Array:
                StringBuilder builder = new();
                foreach (var chunk in delta.EnumerateArray())
                {
                    if (chunk.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(chunk.GetString());
                    }
                    else if (chunk.ValueKind == JsonValueKind.Object &&
                             chunk.TryGetProperty("text", out var chunkText) &&
                             chunkText.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(chunkText.GetString());
                    }
                }

                if (builder.Length > 0)
                {
                    return builder.ToString();
                }
                break;
        }

        return TryGetTopLevelString(root, "delta");
    }

    private static string? TryGetTopLevelString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        return null;
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        ChatGPTResponse? casted = JsonSerializer.Deserialize<ChatGPTResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

        string raw = string.Empty;
        //string raw = casted.output[0].content[0].text;

        if(casted is null || casted.output[0] is null)
        {
            throw new Exception("No content was returned, or the model didn't return in the correct format..");
        }

        foreach (var item in casted.output)
        {
            if(item.content is null)
            {
                continue;
            }

            foreach (var content in item.content)
            {
                if (content is not null)
                {
                    raw = content.text;
                    break;
                }
            }
        }


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
