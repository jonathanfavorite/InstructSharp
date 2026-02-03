using InstructSharp.Clients.DeepSeek;
using InstructSharp.Core;
using InstructSharp.Helpers;
using InstructSharp.Types;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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
        // Only set timeout if no HttpClient was provided (respect user's configured timeout)
        if (!_httpClientWasProvided)
        {
            _httpClient.Timeout = _config.Timeout;
        }
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

    protected override object TransformRequestWithImages<T>(GeminiRequest request)
    {
        throw new NotSupportedException("Image uploads are not currently supported.");
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

    public async Task<GeminiImageGenerationResult> GenerateImageAsync(GeminiImageGenerationRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Validate();

        var parts = new List<object>
        {
            new { text = request.Prompt }
        };

        if (request.Images is { Count: > 0 })
        {
            for (int i = 0; i < request.Images.Count; i++)
            {
                var resolved = ResolveInlineImage(request.Images[i]);
                parts.Add(new
                {
                    inlineData = new
                    {
                        mimeType = resolved.MimeType,
                        data = resolved.Base64Data
                    }
                });
            }
        }

        var generationConfig = GeminiImagePayloadBuilder.BuildGenerationConfig(request);

        var payload = new Dictionary<string, object?>
        {
            ["contents"] = new[]
            {
                new { parts }
            }
        };

        if (generationConfig is { Count: > 0 })
        {
            payload["generationConfig"] = generationConfig;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var requestUrl = BuildRequestUrl(request.Model);
        using var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[GeminiImage] Error {(int)response.StatusCode} {response.StatusCode}: {jsonResponse}");
            response.EnsureSuccessStatusCode();
        }

        var parsed = JsonSerializer.Deserialize<GeminiImageGenerationResponse>(jsonResponse, _jsonOptions)
                     ?? throw new InvalidOperationException("Image generation response could not be parsed.");

        var images = new List<GeminiGeneratedImage>();
        foreach (var candidate in parsed.Candidates)
        {
            if (candidate.Content?.Parts is null)
            {
                continue;
            }

            foreach (var part in candidate.Content.Parts)
            {
                var inlineData = part.InlineData;
                if (inlineData is null || string.IsNullOrWhiteSpace(inlineData.Data))
                {
                    continue;
                }

                images.Add(new GeminiGeneratedImage
                {
                    Base64Data = inlineData.Data,
                    MimeType = inlineData.MimeType
                });
            }
        }

        return new GeminiImageGenerationResult
        {
            Model = request.Model,
            CreatedAt = DateTimeOffset.UtcNow,
            Images = images
        };
    }

    private static InlineImage ResolveInlineImage(LLMImageRequest image)
    {
        if (image is null || string.IsNullOrWhiteSpace(image.Url))
        {
            throw new ArgumentException("Image inputs must include a file path or base64 data URL.", nameof(image));
        }

        if (image.IsBase64)
        {
            return ParseDataUrl(image.Url);
        }

        if (File.Exists(image.Url))
        {
            var bytes = File.ReadAllBytes(image.Url);
            var extension = Path.GetExtension(image.Url);
            var contentType = GetContentTypeFromExtension(extension);
            var base64 = Convert.ToBase64String(bytes);
            return new InlineImage(base64, contentType);
        }

        if (Uri.TryCreate(image.Url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Gemini image generation requires inline image data. Provide a local file path or a base64 data URL.");
        }

        throw new FileNotFoundException($"Image file not found: {image.Url}", image.Url);
    }

    private static InlineImage ParseDataUrl(string dataUrl)
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

        string normalized = LLMRequestImageHelper.StripBase64Prefix(base64);
        return new InlineImage(normalized, contentType);
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

    private readonly record struct InlineImage(string Base64Data, string MimeType);

}
