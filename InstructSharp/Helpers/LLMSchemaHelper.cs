using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using NJsonSchema;
using System.Text.Json;

namespace InstructSharp.Helpers;
internal static class LLMSchemaHelper
{
    // the bulk of this was created by ChatGPT, with some modifications to improve the schema generation
    // after a few hours of trying to do it myself, I caved and let it figure it out for me.
    internal static string GenerateJsonSchema(Type t)
    {
        try
        {
            // Configure the NJsonSchema generator settings.
            var settings = new NJsonSchema.NewtonsoftJson.Generation.NewtonsoftJsonSchemaGeneratorSettings
            {
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                SchemaType = SchemaType.JsonSchema,
                FlattenInheritanceHierarchy = true // Helps remove "allOf" constructs
            };

            // Generate an NJsonSchema for the type.
            JsonSchema nsSchema = JsonSchema.FromType(t, settings);

            // Post-process the schema: disable additional properties and enforce required properties on the root.
            DisallowAdditionalProperties(nsSchema);
            EnforceRequiredProperties(nsSchema);

            // Also enforce required properties on all definitions.
            if (nsSchema.Definitions != null)
            {
                foreach (var def in nsSchema.Definitions.Values)
                {
                    DisallowAdditionalProperties(def);
                    EnforceRequiredProperties(def);
                }
            }

            // Convert the schema to JSON.
            string nsSchemaJson = nsSchema.ToJson();

            // Load into a JObject for further post-processing.
            var jObj = JObject.Parse(nsSchemaJson);

            // Remove unwanted keys: "$schema", "title", "allOf", "format".
            RemovePropertiesRecursively(jObj, new[] { "$schema", "title", "allOf", "format" });
            // Remove "additionalProperties" from any object that has a "$ref"
            RemoveAdditionalPropertiesIfRef(jObj);

            // Ensure that the top-level "type" is explicitly "object".
            jObj["type"] = "object";

            return jObj.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating schema: " + ex);
            throw;
        }
    }

    /// <summary>
    /// Recursively removes all properties with names in propertiesToRemove from the given JToken.
    /// </summary>
    private static void RemovePropertiesRecursively(JToken token, string[] propertiesToRemove)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            // Remove properties matching the names in propertiesToRemove.
            foreach (var prop in obj.Properties().Where(p => propertiesToRemove.Contains(p.Name)).ToList())
            {
                prop.Remove();
            }
            // Process child tokens.
            foreach (var property in obj.Properties())
            {
                RemovePropertiesRecursively(property.Value, propertiesToRemove);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                RemovePropertiesRecursively(item, propertiesToRemove);
            }
        }
    }

    /// <summary>
    /// Recursively removes the "additionalProperties" property from any JObject that contains a "$ref" property.
    /// </summary>
    private static void RemoveAdditionalPropertiesIfRef(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            if (obj.ContainsKey("$ref"))
            {
                obj.Remove("additionalProperties");
            }
            foreach (var property in obj.Properties())
            {
                RemoveAdditionalPropertiesIfRef(property.Value);
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
            {
                RemoveAdditionalPropertiesIfRef(item);
            }
        }
    }

    private static void DisallowAdditionalProperties(JsonSchema schema)
    {
        if (schema == null)
            return;

        schema.AllowAdditionalProperties = false;

        if (schema.Properties != null)
        {
            foreach (var prop in schema.Properties.Values)
            {
                DisallowAdditionalProperties(prop);
            }
        }
        if (schema.Item != null)
            DisallowAdditionalProperties(schema.Item);
        if (schema.AdditionalPropertiesSchema != null)
            DisallowAdditionalProperties(schema.AdditionalPropertiesSchema);
        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                DisallowAdditionalProperties(subSchema);
            }
        }
    }

    /// <summary>
    /// Recursively marks each property as required and populates the RequiredProperties collection.
    /// </summary>
    private static void EnforceRequiredProperties(JsonSchema schema)
    {
        if (schema == null)
            return;

        if (schema.Type.HasFlag(JsonObjectType.Object) && schema.Properties != null && schema.Properties.Any())
        {
            foreach (var kvp in schema.Properties)
            {
                kvp.Value.IsRequired = true;
                EnforceRequiredProperties(kvp.Value);
            }
            schema.RequiredProperties.Clear();
            foreach (var key in schema.Properties.Keys)
            {
                schema.RequiredProperties.Add(key);
            }
        }
        if (schema.Item != null)
            EnforceRequiredProperties(schema.Item);
        if (schema.AdditionalPropertiesSchema != null)
            EnforceRequiredProperties(schema.AdditionalPropertiesSchema);
        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                EnforceRequiredProperties(subSchema);
            }
        }
        if (schema.AnyOf != null)
        {
            foreach (var subSchema in schema.AnyOf)
            {
                EnforceRequiredProperties(subSchema);
            }
        }
        if (schema.OneOf != null)
        {
            foreach (var subSchema in schema.OneOf)
            {
                EnforceRequiredProperties(subSchema);
            }
        }
    }
}
