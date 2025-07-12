namespace MacroMate.WebApi.Features.Swagger;

internal static class SwaggerRegistrationExtensions
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SupportNonNullableReferenceTypes();
            options.SchemaFilter<RequireNonNullablePropertiesSchemaFilter>();
        });

        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
    }

    public static void UseSwagger(this IApplicationBuilder app)
    {
        SwaggerBuilderExtensions.UseSwagger(app);
        app.UseSwaggerUI(options =>
        {
            // Remove the schema definitions from the bottom of the page
            options.DefaultModelsExpandDepth(-1);

            // Setup swagger so it stores auth tokens in browser local storage
            options.ConfigObject.AdditionalItems.Add("persistAuthorization", "true");
        });
    }
}
