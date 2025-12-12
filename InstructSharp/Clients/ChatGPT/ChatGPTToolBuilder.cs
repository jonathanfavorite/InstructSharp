using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using InstructSharp.Helpers;

namespace InstructSharp.Clients.ChatGPT;

/// <summary>
/// Fluent builder for constructing ChatGPT tool specifications.
/// </summary>
public sealed class ChatGPTToolBuilder
{
    private readonly string _type;
    private readonly Dictionary<string, object?> _properties = new(StringComparer.Ordinal);
    private JsonNode? _parameterSchema;

    private ChatGPTToolBuilder(string type)
    {
        _type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Start building a function tool.
    /// </summary>
    public static ChatGPTToolBuilder Function(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Function name is required.", nameof(name));
        }

        var builder = new ChatGPTToolBuilder("function");
        builder._properties["name"] = name;
        return builder;
    }

    /// <summary>
    /// Start building a custom tool type.
    /// </summary>
    public static ChatGPTToolBuilder Custom(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Tool type is required.", nameof(type));
        }

        return new ChatGPTToolBuilder(type);
    }

    public ChatGPTToolBuilder WithDescription(string? description)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            _properties["description"] = description;
        }

        return this;
    }

    /// <summary>
    /// Add a custom property to the tool payload (useful for experimental features).
    /// </summary>
    public ChatGPTToolBuilder WithProperty(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Property name is required.", nameof(key));
        }

        _properties[key] = value;
        return this;
    }

    /// <summary>
    /// Supply a JSON schema representing the tool arguments.
    /// </summary>
    public ChatGPTToolBuilder WithJsonSchema(string jsonSchema)
    {
        if (string.IsNullOrWhiteSpace(jsonSchema))
        {
            throw new ArgumentException("JSON schema cannot be empty.", nameof(jsonSchema));
        }

        _parameterSchema = JsonNode.Parse(jsonSchema)
            ?? throw new InvalidOperationException("Invalid JSON schema.");
        return this;
    }

    /// <summary>
    /// Generate the argument schema from a POCO type.
    /// </summary>
    public ChatGPTToolBuilder WithParameters<TArguments>()
    {
        string schemaJson = LLMSchemaHelper.GenerateJsonSchema(typeof(TArguments));
        _parameterSchema = JsonNode.Parse(schemaJson)
            ?? throw new InvalidOperationException("Failed to build schema for the supplied type.");
        return this;
    }

    /// <summary>
    /// Generate the argument schema from a POCO type provided at runtime.
    /// </summary>
    public ChatGPTToolBuilder WithParameters(Type argumentsType)
    {
        if (argumentsType is null)
        {
            throw new ArgumentNullException(nameof(argumentsType));
        }

        string schemaJson = LLMSchemaHelper.GenerateJsonSchema(argumentsType);
        _parameterSchema = JsonNode.Parse(schemaJson)
            ?? throw new InvalidOperationException("Failed to build schema for the supplied type.");
        return this;
    }

    public ChatGPTToolBuilder WithParameterSchema(JsonNode schemaNode)
    {
        _parameterSchema = schemaNode?.DeepClone();
        return this;
    }

    public ChatGPTToolSpecification Build()
    {
        if (string.IsNullOrWhiteSpace(_type))
        {
            throw new InvalidOperationException("Tool type cannot be empty.");
        }

        if (string.Equals(_type, "function", StringComparison.OrdinalIgnoreCase) &&
            (!_properties.TryGetValue("name", out var nameObj) || string.IsNullOrWhiteSpace(Convert.ToString(nameObj, CultureInfo.InvariantCulture))))
        {
            throw new InvalidOperationException("Function tools require a name.");
        }

        Dictionary<string, object?> parameters = new(StringComparer.Ordinal);
        foreach (var kvp in _properties)
        {
            parameters[kvp.Key] = kvp.Value;
        }

        if (_parameterSchema is not null)
        {
            parameters["parameters"] = _parameterSchema;
        }

        return new ChatGPTToolSpecification
        {
            Type = _type,
            Parameters = parameters
        };
    }
}
