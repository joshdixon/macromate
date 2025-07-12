using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MacroMate.WebApi.Features.Swagger;

internal class RequireNonNullablePropertiesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null)
        {
            return;
        }

        var required = new SortedSet<string>(schema.Required?.ToList() ?? []);

        foreach (KeyValuePair<string, OpenApiSchema> kv in schema.Properties)
        {
            string? propName = kv.Key;
            OpenApiSchema propSchema = kv.Value;

            if (propSchema.Nullable == false)
            {
                required.Add(propName);
            }
        }

        if (required.Count > 0)
        {
            schema.Required = required;
        }
    }
}
