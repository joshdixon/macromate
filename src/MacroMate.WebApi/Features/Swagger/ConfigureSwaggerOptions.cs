using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MacroMate.WebApi.Features.Swagger;

internal class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(string? name, SwaggerGenOptions options) => Configure(options);

    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1",
            new OpenApiInfo
            {
                Title = "Flekt",
                Version = "v1"
            });

        // Configure Swagger to use Bearer token authentication.
        options.AddSecurityDefinition("Bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT", // optional: indicates the expected token format
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Please enter your Bearer Token."
            });

        options.CustomSchemaIds(s => s.ToString().Replace("+", ".").Replace("`", "."));

        // options.CustomOperationIds(e => $"{e.ActionDescriptor.RouteValues["action"]}");
        options.CustomOperationIds(e => e.ActionDescriptor.EndpointMetadata.OfType<IEndpointNameMetadata>().Single().EndpointName);

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new List<string>()
            }
        });
    }
}
