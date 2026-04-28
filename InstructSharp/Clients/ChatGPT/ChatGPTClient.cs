using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Interfaces;
using InstructSharp.Types;
using System.Net.Http;
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
        // Only set timeout if no HttpClient was provided (respect user's configured timeout)
        if (!_httpClientWasProvided)
        {
            _httpClient.Timeout = _config.Timeout;
        }
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    public override LLMProvider GetLLMProvider() => LLMProvider.ChatGPT;
    protected override string GetEndpoint() => "responses";

    public async Task<ChatGPTConversation> CreateConversationAsync(ChatGPTConversationCreateRequest? request = null, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>();
        if (request?.Metadata is { Count: > 0 })
        {
            payload["metadata"] = request.Metadata;
        }

        if (request?.Items is { Count: > 0 })
        {
            payload["items"] = request.Items;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync("conversations", content, cancellationToken);
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[ChatGPTConversation] Error {(int)response.StatusCode} {response.StatusCode}: {jsonResponse}");
            response.EnsureSuccessStatusCode();
        }

        var parsed = JsonSerializer.Deserialize<ChatGPTConversationResponse>(jsonResponse, _jsonOptions)
                     ?? throw new InvalidOperationException("Conversation response could not be parsed.");

        var createdAt = parsed.created_at > 0
            ? DateTimeOffset.FromUnixTimeSeconds(parsed.created_at)
            : DateTimeOffset.UtcNow;

        return new ChatGPTConversation
        {
            Id = parsed.id ?? string.Empty,
            CreatedAt = createdAt,
            Metadata = parsed.metadata
        };
    }

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

        JsonNode? schema = null;
        if (typeof(T) != typeof(string))
        {
            // using a custom object
            string customJsonSchema = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
            schema = JsonNode.Parse(customJsonSchema) ?? throw new InvalidCastException();
        }

        var textPayload = BuildTextPayload(request, schema);
        if (textPayload is not null)
        {
            payload["text"] = textPayload;
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
            ["input"] = input,
            ["stream"] = request.Stream
        };

        ApplyCommonRequestOptions(payload, request);

        var tools = BuildToolsPayload(request);
        if (tools.Count > 0)
        {
            payload["tools"] = tools;
        }

        // 5) **Structured output**: under text.format
        JsonNode? schema = null;
        if (typeof(T) != typeof(string))
        {
            // Generate your JSON schema object
            string schemaJson = LLMSchemaHelper.GenerateJsonSchema(typeof(T));
            schema = JsonNode.Parse(schemaJson)!;
        }

        var textPayload = BuildTextPayload(request, schema);
        if (textPayload is not null)
        {
            payload["text"] = textPayload;
        }

        return payload;
    }

    private static object? BuildTextPayload(ChatGPTRequest request, JsonNode? schemaNode)
    {
        bool hasVerbosity = !string.IsNullOrWhiteSpace(request.TextVerbosity);
        if (schemaNode is null && !hasVerbosity)
        {
            return null;
        }

        var text = new Dictionary<string, object?>();
        if (schemaNode is not null)
        {
            text["format"] = new
            {
                type = "json_schema",
                name = "mySchema",
                schema = schemaNode
            };
        }

        if (hasVerbosity)
        {
            text["verbosity"] = request.TextVerbosity;
        }

        return text;
    }

    private static void ApplyCommonRequestOptions(Dictionary<string, object?> payload, ChatGPTRequest request)
    {
        var reasoningPayload = BuildReasoningPayload(request);
        if (reasoningPayload is not null)
        {
            payload["reasoning"] = reasoningPayload;
        }

        if (SupportsSamplingParameters(request))
        {
            payload["temperature"] = request.Temperature;

            if (request.TopP.HasValue)
            {
                payload["top_p"] = request.TopP.Value;
            }
        }

        if (request.MaxOutputTokens.HasValue)
        {
            payload["max_output_tokens"] = request.MaxOutputTokens.Value;
        }

        if (request.MaxToolCalls.HasValue)
        {
            payload["max_tool_calls"] = request.MaxToolCalls.Value;
        }

        if (request.ParallelToolCalls.HasValue)
        {
            payload["parallel_tool_calls"] = request.ParallelToolCalls.Value;
        }

        if (request.Store.HasValue)
        {
            payload["store"] = request.Store.Value;
        }

        if (request.Background.HasValue)
        {
            payload["background"] = request.Background.Value;
        }

        if (request.Metadata is { Count: > 0 })
        {
            payload["metadata"] = request.Metadata;
        }

        if (!string.IsNullOrWhiteSpace(request.PromptCacheKey))
        {
            payload["prompt_cache_key"] = request.PromptCacheKey;
        }

        if (!string.IsNullOrWhiteSpace(request.PromptCacheRetention))
        {
            payload["prompt_cache_retention"] = request.PromptCacheRetention;
        }

        if (!string.IsNullOrWhiteSpace(request.SafetyIdentifier))
        {
            payload["safety_identifier"] = request.SafetyIdentifier;
        }

        if (!string.IsNullOrWhiteSpace(request.ServiceTier))
        {
            payload["service_tier"] = request.ServiceTier;
        }

        if (!string.IsNullOrWhiteSpace(request.ConversationId))
        {
            payload["conversation"] = request.ConversationId;
        }

        if (!string.IsNullOrWhiteSpace(request.PreviousResponseId))
        {
            if (!string.IsNullOrWhiteSpace(request.ConversationId))
            {
                throw new InvalidOperationException("PreviousResponseId cannot be set when ConversationId is provided.");
            }

            payload["previous_response_id"] = request.PreviousResponseId;
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

        if (string.Equals(normalized, "allowed_tools", StringComparison.OrdinalIgnoreCase))
        {
            if (toolChoice.AllowedTools.Count == 0)
            {
                return null;
            }

            return new Dictionary<string, object?>
            {
                ["type"] = "allowed_tools",
                ["mode"] = string.IsNullOrWhiteSpace(toolChoice.Mode) ? "auto" : toolChoice.Mode,
                ["tools"] = toolChoice.AllowedTools.Select(tool => new Dictionary<string, object?>(tool.Parameters)
                {
                    ["type"] = tool.Type
                }).ToList()
            };
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
                ["type"] = "computer_use_preview"
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

    private static bool SupportsSamplingParameters(ChatGPTRequest request)
    {
        string model = (request.Model ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(model))
        {
            return true;
        }

        if (model.StartsWith("o1", StringComparison.Ordinal) ||
            model.StartsWith("o3", StringComparison.Ordinal) ||
            model.StartsWith("o4", StringComparison.Ordinal))
        {
            return false;
        }

        if (model.StartsWith("gpt-5.2", StringComparison.Ordinal) ||
            model.StartsWith("gpt-5.1", StringComparison.Ordinal))
        {
            return string.Equals(request.Reasoning?.Effort, "none", StringComparison.OrdinalIgnoreCase);
        }

        if (model.StartsWith("gpt-5", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
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

        bool hasImages = request.Images is { Count: > 0 } ||
                         request.ImageReferences is { Count: > 0 } ||
                         request.Mask is not null ||
                         request.MaskReference is not null;
        var endpoint = hasImages ? "images/edits" : "images/generations";
        using HttpContent content = hasImages
            ? BuildImageEditContent(request)
            : BuildImageGenerationContent(request);
        using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[ChatGPTImage] Error {(int)response.StatusCode} {response.StatusCode}: {jsonResponse}");
            response.EnsureSuccessStatusCode();
        }

        if (request.Stream == true)
        {
            return ParseStreamingImageResult(jsonResponse, request);
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
            Background = parsed.Background,
            OutputFormat = parsed.OutputFormat,
            Quality = parsed.Quality,
            Size = parsed.Size,
            Images = images
        };
    }

    private HttpContent BuildImageGenerationContent(ChatGPTImageGenerationRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["prompt"] = request.Prompt,
            ["model"] = request.Model
        };

        AddCommonImageJsonParameters(payload, request);

        var body = JsonSerializer.Serialize(payload, _jsonOptions);
        return new StringContent(body, Encoding.UTF8, "application/json");
    }

    private HttpContent BuildImageEditContent(ChatGPTImageGenerationRequest request)
    {
        if (request.ImageReferences.Count > 0 || request.MaskReference is not null)
        {
            return BuildImageEditJsonContent(request);
        }

        var form = new MultipartFormDataContent();

        form.Add(new StringContent(request.Model), "model");
        form.Add(new StringContent(request.Prompt), "prompt");
        AddCommonImageFormParameters(form, request);

        if (request.Images is null)
        {
            return form;
        }

        for (int i = 0; i < request.Images.Count; i++)
        {
            var image = request.Images[i];
            var resolved = ResolveImageUpload(image, i + 1);
            var imageContent = new ByteArrayContent(resolved.Bytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(resolved.ContentType);
            form.Add(imageContent, "image[]", resolved.FileName);
        }

        if (request.Mask is not null)
        {
            var resolved = ResolveImageUpload(request.Mask, 1);
            var maskContent = new ByteArrayContent(resolved.Bytes);
            maskContent.Headers.ContentType = new MediaTypeHeaderValue(resolved.ContentType);
            form.Add(maskContent, "mask", resolved.FileName);
        }

        return form;
    }

    private HttpContent BuildImageEditJsonContent(ChatGPTImageGenerationRequest request)
    {
        var payload = new Dictionary<string, object?>
        {
            ["prompt"] = request.Prompt,
            ["model"] = request.Model
        };

        AddCommonImageJsonParameters(payload, request);

        var images = new List<Dictionary<string, string>>();
        if (request.ImageReferences.Count > 0)
        {
            foreach (var image in request.ImageReferences)
            {
                images.Add(BuildImageReferencePayload(image));
            }
        }
        else
        {
            for (int i = 0; i < request.Images.Count; i++)
            {
                var resolved = ResolveImageUpload(request.Images[i], i + 1);
                images.Add(new Dictionary<string, string>
                {
                    ["image_url"] = BuildDataUrl(resolved)
                });
            }
        }

        payload["image"] = images;

        if (request.MaskReference is not null)
        {
            payload["mask"] = BuildImageReferencePayload(request.MaskReference);
        }
        else if (request.Mask is not null)
        {
            payload["mask"] = BuildDataUrl(ResolveImageUpload(request.Mask, 1));
        }

        var body = JsonSerializer.Serialize(payload, _jsonOptions);
        return new StringContent(body, Encoding.UTF8, "application/json");
    }

    private static void AddCommonImageJsonParameters(Dictionary<string, object?> payload, ChatGPTImageGenerationRequest request)
    {
        payload["n"] = request.ImageCount;

        AddOptional(payload, "size", request.Size);
        AddOptional(payload, "quality", request.Quality);
        AddOptional(payload, "output_format", request.OutputFormat);
        AddOptional(payload, "style", request.Style);
        AddOptional(payload, "background", request.Background);
        AddOptional(payload, "moderation", request.Moderation);
        AddOptional(payload, "response_format", request.ResponseFormat);
        AddOptional(payload, "input_fidelity", request.InputFidelity);
        AddOptional(payload, "user", request.User);

        if (request.OutputCompression.HasValue)
        {
            payload["output_compression"] = request.OutputCompression.Value;
        }

        if (request.PartialImages.HasValue)
        {
            payload["partial_images"] = request.PartialImages.Value;
        }

        if (request.Stream.HasValue)
        {
            payload["stream"] = request.Stream.Value;
        }

        if (request.Seed.HasValue)
        {
            payload["seed"] = request.Seed.Value;
        }
    }

    private static void AddCommonImageFormParameters(MultipartFormDataContent form, ChatGPTImageGenerationRequest request)
    {
        form.Add(new StringContent(request.ImageCount.ToString(CultureInfo.InvariantCulture)), "n");

        AddOptional(form, "size", request.Size);
        AddOptional(form, "quality", request.Quality);
        AddOptional(form, "output_format", request.OutputFormat);
        AddOptional(form, "style", request.Style);
        AddOptional(form, "background", request.Background);
        AddOptional(form, "moderation", request.Moderation);
        AddOptional(form, "response_format", request.ResponseFormat);
        AddOptional(form, "input_fidelity", request.InputFidelity);
        AddOptional(form, "user", request.User);

        if (request.OutputCompression.HasValue)
        {
            form.Add(new StringContent(request.OutputCompression.Value.ToString(CultureInfo.InvariantCulture)), "output_compression");
        }

        if (request.PartialImages.HasValue)
        {
            form.Add(new StringContent(request.PartialImages.Value.ToString(CultureInfo.InvariantCulture)), "partial_images");
        }

        if (request.Stream.HasValue)
        {
            form.Add(new StringContent(request.Stream.Value ? "true" : "false"), "stream");
        }

        if (request.Seed.HasValue)
        {
            form.Add(new StringContent(request.Seed.Value.ToString(CultureInfo.InvariantCulture)), "seed");
        }
    }

    private static void AddOptional(Dictionary<string, object?> payload, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            payload[name] = value;
        }
    }

    private static void AddOptional(MultipartFormDataContent form, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            form.Add(new StringContent(value), name);
        }
    }

    private static Dictionary<string, string> BuildImageReferencePayload(ChatGPTImageReference reference)
    {
        if (!string.IsNullOrWhiteSpace(reference.FileId))
        {
            return new Dictionary<string, string> { ["file_id"] = reference.FileId };
        }

        return new Dictionary<string, string> { ["image_url"] = reference.ImageUrl ?? string.Empty };
    }

    private static string BuildDataUrl(ImageUpload upload)
    {
        return $"data:{upload.ContentType};base64,{Convert.ToBase64String(upload.Bytes)}";
    }

    private static ChatGPTImageGenerationResult ParseStreamingImageResult(string responseBody, ChatGPTImageGenerationRequest request)
    {
        var images = new List<ChatGPTGeneratedImage>();
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        using var reader = new StringReader(responseBody);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (payload.Length == 0 || payload == "[DONE]")
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                var type = TryGetTopLevelString(root, "type");
                if (root.TryGetProperty("created", out var created) && created.ValueKind == JsonValueKind.Number && created.TryGetInt64(out long unixTime))
                {
                    createdAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                }

                string? b64 = TryGetTopLevelString(root, "b64_json");
                if (string.IsNullOrWhiteSpace(b64))
                {
                    b64 = TryGetTopLevelString(root, "partial_image_b64");
                }

                string? url = TryGetTopLevelString(root, "url");
                if (string.IsNullOrWhiteSpace(b64) && string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                int? partialImageIndex = null;
                if (root.TryGetProperty("partial_image_index", out var partialIndex) &&
                    partialIndex.ValueKind == JsonValueKind.Number &&
                    partialIndex.TryGetInt32(out int parsedIndex))
                {
                    partialImageIndex = parsedIndex;
                }

                images.Add(new ChatGPTGeneratedImage
                {
                    Base64Data = b64,
                    Url = url,
                    PartialImageIndex = partialImageIndex,
                    EventType = type
                });
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return new ChatGPTImageGenerationResult
        {
            Model = request.Model,
            CreatedAt = createdAt,
            Images = images
        };
    }

    private static ImageUpload ResolveImageUpload(LLMImageRequest image, int index)
    {
        if (image is null || string.IsNullOrWhiteSpace(image.Url))
        {
            throw new ArgumentException("Image inputs must include a file path or base64 data URL.", nameof(image));
        }

        if (image.IsBase64)
        {
            return ParseDataUrl(image.Url, index);
        }

        if (File.Exists(image.Url))
        {
            var bytes = File.ReadAllBytes(image.Url);
            var extension = Path.GetExtension(image.Url);
            var contentType = GetContentTypeFromExtension(extension);
            var fileName = Path.GetFileName(image.Url);
            return new ImageUpload(bytes, contentType, string.IsNullOrWhiteSpace(fileName) ? $"image_{index}{extension}" : fileName);
        }

        if (Uri.TryCreate(image.Url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Image edits require file uploads. Provide a local file path or a base64 data URL.");
        }

        throw new FileNotFoundException($"Image file not found: {image.Url}", image.Url);
    }

    private static ImageUpload ParseDataUrl(string dataUrl, int index)
    {
        int commaIndex = dataUrl.IndexOf(',');
        if (commaIndex < 0)
        {
            throw new FormatException("Invalid data URL for image input.");
        }

        string header = dataUrl.Substring(0, commaIndex);
        string base64 = dataUrl.Substring(commaIndex + 1);

        string contentType = "application/octet-stream";
        if (header.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            int semiIndex = header.IndexOf(';');
            if (semiIndex > "data:".Length)
            {
                contentType = header.Substring("data:".Length, semiIndex - "data:".Length);
            }
        }

        var bytes = Convert.FromBase64String(LLMRequestImageHelper.StripBase64Prefix(base64));
        var extension = GetExtensionFromContentType(contentType);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = "png";
            contentType = "image/png";
        }

        string fileName = $"image_{index}.{extension}";
        return new ImageUpload(bytes, contentType, fileName);
    }

    private static string GetContentTypeFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "application/octet-stream";
        }

        string normalized = extension.StartsWith('.') ? extension.Substring(1) : extension;
        normalized = normalized.ToLowerInvariant();

        return normalized switch
        {
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            "gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    private static string? GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => null
        };
    }

    private readonly record struct ImageUpload(byte[] Bytes, string ContentType, string FileName);

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
        if (!string.IsNullOrWhiteSpace(response.output_text))
        {
            return response.output_text;
        }

        if (response.output is null)
        {
            return string.Empty;
        }

        foreach (var item in response.output)
        {
            if (item is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(item.text))
            {
                return item.text;
            }

            if (item.content is null)
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

            bool hasToolCall = item.function_call is not null || item.tool_call is not null;
            if (!hasToolCall &&
                !string.Equals(item.type, "function_call", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.type, "tool_call", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? arguments = !string.IsNullOrWhiteSpace(item.arguments)
                ? item.arguments
                : item.tool_call?.arguments ?? item.function_call?.arguments;
            string? name = !string.IsNullOrWhiteSpace(item.name)
                ? item.name
                : item.tool_call?.name ?? item.function_call?.name;
            string? output = item.tool_call?.output ?? item.function_call?.output;

            calls.Add(new ChatGPTToolCall
            {
                Id = item.id ?? item.call_id ?? string.Empty,
                CallId = item.call_id ?? item.id,
                Type = item.type ?? "function_call",
                Name = name,
                ArgumentsJson = arguments,
                Output = output,
                Status = item.status
            });
        }

        return calls;
    }
}
