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
using System.Linq;

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
            if (string.Equals(normalized, "function", StringComparison.OrdinalIgnoreCase))
            {
                payload["name"] = toolChoice.FunctionName;
            }
            else
            {
                payload["function_name"] = toolChoice.FunctionName;
            }
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

        if(!response.IsSuccessStatusCode)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
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
            var toolCall = ExtractToolCall(root);
            var toolType = toolCall?.Type;
            var toolId = toolCall?.CallId ?? toolCall?.Id;
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
                ToolCall = toolCall,
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

    private static ChatGPTToolCall? ExtractToolCall(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (root.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
        {
            var fromDelta = ExtractToolCallFromDelta(delta);
            if (fromDelta is not null)
            {
                return fromDelta;
            }
        }

        if (root.TryGetProperty("tool_call", out var toolCallElem) && toolCallElem.ValueKind == JsonValueKind.Object)
        {
            return ParseToolCallElement(toolCallElem);
        }

        var functionCallArgs = TryParseFunctionCallArguments(root);
        if (functionCallArgs is not null)
        {
            return functionCallArgs;
        }

        if (root.TryGetProperty("function_call", out var functionCall) && functionCall.ValueKind == JsonValueKind.Object)
        {
            return ParseFunctionCallElement(root, functionCall);
        }

        if (root.TryGetProperty("web_search_call", out var webSearchCall) && webSearchCall.ValueKind == JsonValueKind.Object)
        {
            return ParseNonFunctionToolCall(webSearchCall, "web_search");
        }

        if (root.TryGetProperty("output", out var outputElem) && outputElem.ValueKind == JsonValueKind.Object)
        {
            if (outputElem.TryGetProperty("tool_call", out var nestedToolCall) && nestedToolCall.ValueKind == JsonValueKind.Object)
            {
                return ParseToolCallElement(nestedToolCall);
            }
        }

        return null;
    }

    private static ChatGPTToolCall? ExtractToolCallFromDelta(JsonElement delta)
    {
        if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in toolCalls.EnumerateArray())
            {
                var parsed = ParseToolCallElement(call);
                if (parsed is not null)
                {
                    return parsed;
                }
            }
        }

        if (delta.TryGetProperty("web_search_call", out var webSearchCall) && webSearchCall.ValueKind == JsonValueKind.Object)
        {
            return ParseNonFunctionToolCall(webSearchCall, "web_search");
        }

        var functionCallArgs = TryParseFunctionCallArguments(delta);
        if (functionCallArgs is not null)
        {
            return functionCallArgs;
        }

        if (delta.TryGetProperty("function_call", out var functionCall) && functionCall.ValueKind == JsonValueKind.Object)
        {
            return ParseFunctionCallElement(delta, functionCall);
        }

        return null;
    }

    private static ChatGPTToolCall? ParseToolCallElement(JsonElement toolCallElement)
    {
        if (toolCallElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? type = TryGetString(toolCallElement, "type");
        string? id = TryGetString(toolCallElement, "id");
        string? callId = TryGetString(toolCallElement, "call_id");
        string? status = TryGetString(toolCallElement, "status");

        string? name = null;
        string? arguments = null;
        string? output = null;

        if (toolCallElement.TryGetProperty("function", out var functionElem) && functionElem.ValueKind == JsonValueKind.Object)
        {
            name = TryGetString(functionElem, "name");
            arguments = TryGetJsonAsString(functionElem, "arguments");
            output = TryGetJsonAsString(functionElem, "output");
        }
        else
        {
            name = TryGetString(toolCallElement, "name");
            arguments = TryGetJsonAsString(toolCallElement, "arguments");
            output = TryGetJsonAsString(toolCallElement, "output");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            type = "function";
        }

        return new ChatGPTToolCall
        {
            Id = id ?? callId ?? string.Empty,
            CallId = callId ?? id,
            Type = type,
            Name = name,
            ArgumentsJson = arguments,
            Output = output,
            Status = status
        };
    }

    private static ChatGPTToolCall ParseFunctionCallElement(JsonElement root, JsonElement functionCall)
    {
        var callId = TryGetString(root, "call_id") ?? TryGetString(root, "id");
        var name = TryGetString(functionCall, "name");
        var arguments = TryGetJsonAsString(functionCall, "arguments");
        var output = TryGetJsonAsString(functionCall, "output");
        return new ChatGPTToolCall
        {
            Id = callId ?? string.Empty,
            CallId = callId,
            Type = "function",
            Name = name,
            ArgumentsJson = arguments,
            Output = output,
            Status = TryGetString(root, "status")
        };
    }

    private static ChatGPTToolCall? TryParseFunctionCallArguments(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        JsonElement? argsProp = null;
        if (element.TryGetProperty("function_call_arguments", out var fcArgs))
        {
            argsProp = fcArgs;
        }
        else if (element.TryGetProperty("arguments", out var finalArgs))
        {
            argsProp = finalArgs;
        }

        string? arguments = null;
        if (argsProp.HasValue)
        {
            arguments = argsProp.Value.ValueKind switch
            {
                JsonValueKind.String => argsProp.Value.GetString(),
                JsonValueKind.Object or JsonValueKind.Array => argsProp.Value.GetRawText(),
                _ => null
            };
        }

        string? delta = TryGetString(element, "delta");
        if (string.IsNullOrWhiteSpace(arguments))
        {
            arguments = delta;
        }

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        string? id = TryGetString(element, "item_id") ?? TryGetString(element, "id");
        string? callId = TryGetString(element, "call_id") ?? id;

        int? outputIndex = null;
        if (element.TryGetProperty("output_index", out var idx) &&
            idx.ValueKind == JsonValueKind.Number &&
            idx.TryGetInt32(out var parsedIdx))
        {
            outputIndex = parsedIdx;
        }

        string? name = TryGetString(element, "name");

        return new ChatGPTToolCall
        {
            Id = id ?? callId ?? string.Empty,
            CallId = callId,
            Type = "function",
            Name = name,
            ArgumentsJson = arguments,
            RawItemId = id,
            OutputIndex = outputIndex
        };
    }

    private static ChatGPTToolCall ParseNonFunctionToolCall(JsonElement element, string type)
    {
        var callId = TryGetString(element, "call_id");
        return new ChatGPTToolCall
        {
            Id = callId ?? string.Empty,
            CallId = callId,
            Type = type,
            ArgumentsJson = element.GetRawText()
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;
    }

    private static string? TryGetJsonAsString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Object or JsonValueKind.Array => prop.GetRawText(),
            _ => null
        };
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
            "response.function_call_arguments.delta" => ChatGPTStreamEventType.ResponseToolCallDelta,
            "response.function_call_arguments.done" => ChatGPTStreamEventType.ResponseToolCallDone,
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

    public async Task<ChatGPTImageGenerationResult> GenerateImageAsync(ChatGPTImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Validate();

        var payload = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["prompt"] = request.Prompt,
            ["size"] = request.Size,
            ["quality"] = request.Quality,
            ["n"] = request.ImageCount
        };
        if (!string.IsNullOrWhiteSpace(request.OutputFormat))
        {
            payload["output_format"] = request.OutputFormat;
        }
        if (!string.IsNullOrWhiteSpace(request.Style))
        {
            payload["style"] = request.Style;
        }

        if (!string.IsNullOrWhiteSpace(request.Background))
        {
            payload["background"] = request.Background;
        }

        // 'negative_prompt' is not yet accepted by GPT-Image; reserved for future parity.

        if (!string.IsNullOrWhiteSpace(request.User))
        {
            payload["user"] = request.User;
        }

        if (request.Seed.HasValue)
        {
            payload["seed"] = request.Seed;
        }

        var body = JsonSerializer.Serialize(payload, _jsonOptions);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("images/generations", content, cancellationToken);
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[ChatGPTImage] Error {(int)response.StatusCode} {response.StatusCode}: {jsonResponse}");
            response.EnsureSuccessStatusCode();
        }

        var parsed = JsonSerializer.Deserialize<ChatGPTImageGenerationResponse>(jsonResponse, _jsonOptions)
                     ?? throw new InvalidOperationException("Image generation response could not be parsed.");

        var createdAt = parsed.Created > 0
            ? DateTimeOffset.FromUnixTimeSeconds(parsed.Created)
            : DateTimeOffset.UtcNow;

        var images = parsed.Data.Select(d => new ChatGPTGeneratedImage
        {
            Base64Data = d.Base64Payload,
            Url = d.Url,
            RevisedPrompt = d.RevisedPrompt ?? parsed.RevisedPrompt
        }).ToList();

        return new ChatGPTImageGenerationResult
        {
            Model = request.Model,
            CreatedAt = createdAt,
            Images = images
        };
    }

    protected override LLMResponse<T> TransformResponse<T>(string jsonResponse)
    {
        ChatGPTResponse? casted = JsonSerializer.Deserialize<ChatGPTResponse>(jsonResponse, _jsonOptions) ?? throw new InvalidOperationException("Empty response");

        string raw = ExtractTextFromResponse(casted);
        IReadOnlyList<ChatGPTToolCall> toolCalls = ExtractToolCalls(casted);

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

        if (toolCalls.Count > 0)
        {
            response.AdditionalData[ChatGPTToolCall.AdditionalDataKey] = toolCalls;
        }

        if (typeof(T) == typeof(string))
        {
            response.Result = (T)(object)(raw ?? string.Empty);
            return response;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            response.Result = default;
            return response;
        }

        response.Result = JsonSerializer.Deserialize<T>(raw)
            ?? throw new InvalidOperationException($"Failed to deserialize response into type {typeof(T).Name}");
        return response;
    }

    private static string ExtractTextFromResponse(ChatGPTResponse response)
    {
        if (response.output is null)
        {
            return string.Empty;
        }

        foreach (var item in response.output)
        {
            if (item?.content is null)
            {
                continue;
            }

            foreach (var content in item.content)
            {
                if (!string.IsNullOrWhiteSpace(content?.text))
                {
                    return content.text;
                }
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<ChatGPTToolCall> ExtractToolCalls(ChatGPTResponse response)
    {
        List<ChatGPTToolCall> calls = new();
        if (response.output is null)
        {
            return calls;
        }

        foreach (var item in response.output)
        {
            if (item is null)
            {
                continue;
            }

            if (!string.Equals(item.type, "function_call", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? arguments = !string.IsNullOrWhiteSpace(item.arguments)
                ? item.arguments
                : item.function_call?.arguments;
            string? name = !string.IsNullOrWhiteSpace(item.name)
                ? item.name
                : item.function_call?.name;

            calls.Add(new ChatGPTToolCall
            {
                Id = item.id ?? item.call_id ?? string.Empty,
                CallId = item.call_id ?? item.id,
                Type = item.type ?? "function_call",
                Name = name,
                ArgumentsJson = arguments,
                Output = item.function_call?.output,
                Status = item.status
            });
        }

        return calls;
    }
}
