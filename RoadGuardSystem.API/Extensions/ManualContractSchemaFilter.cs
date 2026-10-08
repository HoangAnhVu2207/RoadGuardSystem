using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RoadGuardSystem.API.Extensions;

public sealed class ManualContractSchemaFilter : ISchemaFilter
{
    internal static readonly JsonElement Metadata = Load("RoadGuardSystem.API.ManualSwaggerSchemas.json");

    internal static JsonElement Load(string name)
    {
        using var stream = typeof(ManualContractSchemaFilter).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Missing Swagger schema audit: " + name);
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.Clone();
    }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (Metadata.GetProperty("componentOverrides").TryGetProperty(context.Type.FullName ?? "", out var component))
            foreach (var property in component.GetProperty("properties").EnumerateObject())
                schema.Properties[property.Name] = Shape(property.Value, context.SchemaGenerator, context.SchemaRepository);
        foreach (var rule in Metadata.GetProperty("properties").EnumerateArray().Where(x => x.GetProperty("dto").GetString() == context.Type.FullName))
        {
            var name = rule.GetProperty("property").GetString()!;
            if (!schema.Properties.TryGetValue(name, out var property)) throw new InvalidOperationException("Audited Swagger property missing: " + context.Type.FullName + "." + name);
            Constraints(property, rule);
            if (rule.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True) schema.Required.Add(name);
        }
    }

    internal static void Constraints(OpenApiSchema schema, JsonElement rule)
    {
        if (rule.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
            schema.Description = string.Join(" ", new[] { schema.Description, description.GetString() }.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (rule.TryGetProperty("enum", out var enumeration) && enumeration.ValueKind == JsonValueKind.Array)
            schema.Enum = enumeration.EnumerateArray().Select(Value).ToList();
        if (rule.TryGetProperty("pattern", out var pattern) && pattern.ValueKind == JsonValueKind.String) schema.Pattern = pattern.GetString();
        if (rule.TryGetProperty("format", out var format) && format.ValueKind == JsonValueKind.String) schema.Format = format.GetString();
        if (rule.TryGetProperty("nullable", out var nullable) && nullable.ValueKind is JsonValueKind.True or JsonValueKind.False) schema.Nullable = nullable.GetBoolean();
        if (rule.TryGetProperty("minimum", out var min) && min.ValueKind == JsonValueKind.Number) schema.Minimum = min.GetDecimal();
        if (rule.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number) schema.Maximum = max.GetDecimal();
        if (rule.TryGetProperty("exclusiveMinimum", out var exMin) && exMin.ValueKind is JsonValueKind.True or JsonValueKind.False) schema.ExclusiveMinimum = exMin.GetBoolean();
        if (rule.TryGetProperty("exclusiveMaximum", out var exMax) && exMax.ValueKind is JsonValueKind.True or JsonValueKind.False) schema.ExclusiveMaximum = exMax.GetBoolean();
        if (rule.TryGetProperty("minLength", out var minLength) && minLength.ValueKind == JsonValueKind.Number) schema.MinLength = minLength.GetInt32();
        if (rule.TryGetProperty("maxLength", out var maxLength) && maxLength.ValueKind == JsonValueKind.Number) schema.MaxLength = maxLength.GetInt32();
        if (rule.TryGetProperty("minItems", out var minItems) && minItems.ValueKind == JsonValueKind.Number) schema.MinItems = minItems.GetInt32();
        if (rule.TryGetProperty("maxItems", out var maxItems) && maxItems.ValueKind == JsonValueKind.Number) schema.MaxItems = maxItems.GetInt32();
        if (schema.Items is not null)
        {
            if (rule.TryGetProperty("itemMinimum", out var itemMin) && itemMin.ValueKind == JsonValueKind.Number) schema.Items.Minimum = itemMin.GetDecimal();
            if (rule.TryGetProperty("itemMaximum", out var itemMax) && itemMax.ValueKind == JsonValueKind.Number) schema.Items.Maximum = itemMax.GetDecimal();
        }
        if (rule.TryGetProperty("crossElementConstraints", out var cross) && cross.ValueKind == JsonValueKind.Array)
        {
            var constraints = new OpenApiArray();
            foreach (var item in cross.EnumerateArray()) constraints.Add(new OpenApiString(item.GetString()));
            schema.Extensions["x-roadguard-cross-element-constraints"] = constraints;
        }
    }

    private static IOpenApiAny Value(JsonElement item) => item.ValueKind switch
    {
        JsonValueKind.String => new OpenApiString(item.GetString()),
        JsonValueKind.Number => item.TryGetInt32(out var number) ? new OpenApiInteger(number) : new OpenApiDouble(item.GetDouble()),
        JsonValueKind.True => new OpenApiBoolean(true),
        JsonValueKind.False => new OpenApiBoolean(false),
        _ => new OpenApiNull()
    };

    internal static OpenApiSchema Shape(JsonElement shape, OperationFilterContext context)
        => Shape(shape, context.SchemaGenerator, context.SchemaRepository);

    private static OpenApiSchema Shape(JsonElement shape, ISchemaGenerator generator, SchemaRepository repository)
    {
        if (shape.TryGetProperty("clrType", out var clrType))
        {
            var name = clrType.GetString()!;
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(name)).FirstOrDefault(x => x is not null)
                ?? throw new InvalidOperationException("Swagger shape type not found: " + name);
            return generator.GenerateSchema(type, repository);
        }
        var schema = new OpenApiSchema();
        if (shape.TryGetProperty("type", out var typeElement)) schema.Type = typeElement.GetString();
        if (shape.TryGetProperty("format", out var format)) schema.Format = format.GetString();
        Constraints(schema, shape);
        if (shape.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
            schema.Required = required.EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal);
        if (shape.TryGetProperty("properties", out var properties))
            foreach (var property in properties.EnumerateObject()) schema.Properties[property.Name] = Shape(property.Value, generator, repository);
        if (shape.TryGetProperty("items", out var items)) schema.Items = Shape(items, generator, repository);
        if (shape.TryGetProperty("oneOf", out var oneOf)) schema.OneOf = oneOf.EnumerateArray().Select(x => Shape(x, generator, repository)).ToList();
        if (shape.TryGetProperty("allOf", out var allOf)) schema.AllOf = allOf.EnumerateArray().Select(x => Shape(x, generator, repository)).ToList();
        if (shape.TryGetProperty("anyOf", out var anyOf)) schema.AnyOf = anyOf.EnumerateArray().Select(x => Shape(x, generator, repository)).ToList();
        if (shape.TryGetProperty("additionalProperties", out var additional) && additional.ValueKind == JsonValueKind.Object) schema.AdditionalProperties = Shape(additional, generator, repository);
        else if (additional.ValueKind is JsonValueKind.False or JsonValueKind.True) schema.AdditionalPropertiesAllowed = additional.GetBoolean();
        return schema;
    }
}
