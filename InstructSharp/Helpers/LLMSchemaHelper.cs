using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;
using NJsonSchema;
using System.Text.Json;

namespace InstructSharp.Helpers;
internal static class LLMSchemaHelper
{
    internal static string GenerateJsonSchema(Type t)
    {
        try
        {
            var settings = new NJsonSchema.NewtonsoftJson.Generation.NewtonsoftJsonSchemaGeneratorSettings
            {
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                SchemaType = SchemaType.JsonSchema,
                FlattenInheritanceHierarchy = true
            };

            JsonSchema nsSchema = JsonSchema.FromType(t, settings);

            DisallowAdditionalProperties(nsSchema);
            EnforceRequiredProperties(nsSchema);

            if (nsSchema.Definitions != null)
            {
                foreach (var def in nsSchema.Definitions.Values)
                {
                    DisallowAdditionalProperties(def);
                    EnforceRequiredProperties(def);
                }
            }

            var jObj = JObject.Parse(nsSchema.ToJson());

            // 1) Inline all $ref (and delete #/definitions)
            DerefRefsInPlace(jObj);

            // 2) Flatten oneOf/anyOf unions that slipped in
            FlattenVariants(jObj);

            // 3) Strip banned keys + any stray $ref
            RemovePropertiesRecursively(jObj, new[] { "$schema", "title", "allOf", "anyOf", "oneOf", "format", "default", "nullable", "$ref" });

            // 4) Ensure explicit types
            EnsureObjectArrayTypes(jObj);

            // 5) Keep additionalProperties only on objects
            RemoveAdditionalPropsFromNonObjects(jObj);

            jObj["type"] = "object";
            return jObj.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating schema: " + ex);
            throw;
        }
    }

    private static void DisallowAdditionalProperties(JsonSchema schema)
    {
        if (schema == null) return;
        schema.AllowAdditionalProperties = false;

        if (schema.Properties != null)
            foreach (var prop in schema.Properties.Values)
                DisallowAdditionalProperties(prop);

        if (schema.Item != null)
            DisallowAdditionalProperties(schema.Item);
        if (schema.AdditionalPropertiesSchema != null)
            DisallowAdditionalProperties(schema.AdditionalPropertiesSchema);
        if (schema.AllOf != null)
            foreach (var subSchema in schema.AllOf)
                DisallowAdditionalProperties(subSchema);
    }

    private static void EnforceRequiredProperties(JsonSchema schema)
    {
        if (schema == null) return;

        if (schema.Type.HasFlag(JsonObjectType.Object) && schema.Properties != null && schema.Properties.Any())
        {
            foreach (var kvp in schema.Properties)
            {
                kvp.Value.IsRequired = true;
                EnforceRequiredProperties(kvp.Value);
            }
            schema.RequiredProperties.Clear();
            foreach (var key in schema.Properties.Keys)
                schema.RequiredProperties.Add(key);
        }

        if (schema.Item != null)
            EnforceRequiredProperties(schema.Item);
        if (schema.AdditionalPropertiesSchema != null)
            EnforceRequiredProperties(schema.AdditionalPropertiesSchema);
        if (schema.AllOf != null)
            foreach (var subSchema in schema.AllOf)
                EnforceRequiredProperties(subSchema);
        if (schema.AnyOf != null)
            foreach (var subSchema in schema.AnyOf)
                EnforceRequiredProperties(subSchema);
        if (schema.OneOf != null)
            foreach (var subSchema in schema.OneOf)
                EnforceRequiredProperties(subSchema);
    }

    private static void DerefRefsInPlace(JObject root)
    {
        var defs = (root["definitions"] as JObject)?.Properties()
                    .ToDictionary(p => "#/definitions/" + p.Name, p => (JObject)p.Value.DeepClone())
                  ?? new Dictionary<string, JObject>();

        void Recurse(JToken t)
        {
            if (t is JObject o)
            {
                if (o.TryGetValue("$ref", out var r) && r.Type == JTokenType.String)
                {
                    var key = r.Value<string>();
                    if (key != null && defs.TryGetValue(key, out var def))
                    {
                        o.Remove("$ref");
                        foreach (var p in def.Properties())
                            o[p.Name] = p.Value.DeepClone();
                    }
                }
                foreach (var p in o.Properties().ToList())
                    Recurse(p.Value);
            }
            else if (t is JArray a)
            {
                foreach (var c in a) Recurse(c);
            }
        }

        Recurse(root);
        root.Remove("definitions");
    }

    private static void FlattenVariants(JToken token)
    {
        if (token is JObject obj)
        {
            if (TryGetVariantArray(obj, out var arr))
            {
                var choice = arr.Children<JObject>()
                                .FirstOrDefault(o => o["type"]?.ToString() != "null")
                             ?? arr.Children<JObject>().OfType<JObject>().FirstOrDefault();

                obj.Remove("oneOf");
                obj.Remove("anyOf");

                if (choice != null)
                {
                    foreach (var p in choice.Properties().ToList())
                        obj[p.Name] = p.Value.DeepClone();
                }
            }

            foreach (var p in obj.Properties().ToList())
                FlattenVariants(p.Value);
        }
        else if (token is JArray a)
        {
            foreach (var c in a) FlattenVariants(c);
        }
    }

    private static bool TryGetVariantArray(JObject obj, out JArray arr)
    {
        arr = null!;
        if (obj.TryGetValue("oneOf", out var one) && one is JArray a1) { arr = a1; return true; }
        if (obj.TryGetValue("anyOf", out var any) && any is JArray a2) { arr = a2; return true; }
        return false;
    }

    private static void RemovePropertiesRecursively(JToken token, string[] propertiesToRemove)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;

            foreach (var prop in obj.Properties().Where(p => propertiesToRemove.Contains(p.Name)).ToList())
                prop.Remove();

            foreach (var property in obj.Properties())
                RemovePropertiesRecursively(property.Value, propertiesToRemove);
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
                RemovePropertiesRecursively(item, propertiesToRemove);
        }
    }

    private static void RemoveAdditionalPropertiesIfRef(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var obj = (JObject)token;
            if (obj.ContainsKey("$ref"))
                obj.Remove("additionalProperties");

            foreach (var property in obj.Properties())
                RemoveAdditionalPropertiesIfRef(property.Value);
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token.Children())
                RemoveAdditionalPropertiesIfRef(item);
        }
    }

    private static void RemoveAdditionalPropsFromNonObjects(JToken token)
    {
        if (token is JObject o)
        {
            var type = o["type"]?.ToString();
            if (type != "object")
                o.Remove("additionalProperties");

            foreach (var p in o.Properties().ToList())
                RemoveAdditionalPropsFromNonObjects(p.Value);
        }
        else if (token is JArray a)
        {
            foreach (var c in a) RemoveAdditionalPropsFromNonObjects(c);
        }
    }

    private static void EnsureObjectArrayTypes(JToken token)
    {
        if (token is JObject obj)
        {
            if (obj["properties"] is JObject && obj["type"] == null) obj["type"] = "object";
            if (obj["items"] is JObject && obj["type"]?.ToString() != "array" && obj["$ref"] == null)
                obj["type"] = "array";

            foreach (var p in obj.Properties().ToList())
                EnsureObjectArrayTypes(p.Value);
        }
        else if (token is JArray a)
        {
            foreach (var c in a) EnsureObjectArrayTypes(c);
        }
    }
}
