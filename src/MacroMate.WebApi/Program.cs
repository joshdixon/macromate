using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

using Carter;

using FluentValidation;

using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

using JasperFx;
using JasperFx.Events.Projections;

using Marten;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

using MacroMate.WebApi.Features.Swagger;
using MacroMate.WebApi.Features.Users.Options;
using MacroMate.WebApi.Features.Users.Projections;

using SharpGrip.FluentValidation.AutoValidation.Endpoints.Extensions;
using SharpGrip.FluentValidation.AutoValidation.Endpoints.Filters;

using Wolverine;
using Wolverine.Marten;
using Wolverine.RabbitMQ;

using WorkOS;

[assembly: InternalsVisibleTo("Marten")]

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddSwagger();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>(
    includeInternalTypes: true);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost3000",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Allow requests from localhost:3000
                .AllowAnyHeader() // Allow all headers
                .AllowAnyMethod() // Allow all HTTP methods (GET, POST, etc.)
                .AllowCredentials();
        });
});

builder.Services
    .AddMarten(options =>
    {
        options.Connection(builder.Configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("No Postgres connection string"));
        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.UseSystemTextJsonForSerialization();
        
        options.Projections.Add<UserViewProjection>(ProjectionLifecycle.Inline);

        options.DisableNpgsqlLogging = true;
    })
    .ApplyAllDatabaseChangesOnStartup()
    .UseLightweightSessions()
    .IntegrateWithWolverine();

builder.Host
    .UseWolverine(options =>
    {
        options.UseRabbitMq("amqp://rabbitmq:5672")
            .AutoProvision();
    });

builder.Services.AddCarter(configurator: options =>
{
    // Discover all CarterModule types in the current assembly (including internal)
    Type[] internalModules = typeof(Program).Assembly
        .GetTypes()
        .Where(t => typeof(ICarterModule).IsAssignableFrom(t) && !t.IsAbstract)
        .ToArray();

    // Explicitly register them
    options.WithModules(internalModules);
});

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations();

builder.Services.AddOptions<WorkOsSettings>()
    .Bind(builder.Configuration.GetSection("WorkOS"))
    .ValidateDataAnnotations();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<JwtSecurityTokenHandler>();
builder.Services.AddHttpContextAccessor();

// Provide ISystemClock for scheduled jobs
builder.Services.AddSingleton<ISystemClock, SystemClock>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                StringValues accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                PathString path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/events"))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            // ValidIssuer = jwtOptions.Value.Issuer,
            // ValidAudience = jwtOptions.Value.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.PrivateKey))
        };
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<SSOService>(_ =>
{
    WorkOS.WorkOS.SetApiKey(builder.Configuration.GetSection("WorkOS")["ApiKey"] ?? throw new InvalidOperationException("No WorkOS API key"));

    return new SSOService();
});
builder.Services.AddSingleton<OrganizationsService>(_ => new OrganizationsService());

builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"));
        });
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"webapi-{Environment.MachineName}";
    options.WorkerCount = Environment.ProcessorCount;
});

WebApplication app = builder.Build();
app
    .MapGroup("/")
    .RequireCors("AllowLocalhost3000")
    .AddEndpointFilterFactory((ctx, next) =>
    {
        var filter = new FluentValidationAutoValidationEndpointFilter();

        return ic => filter.InvokeAsync(ic, next);
    })
    .MapCarter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new IDashboardAuthorizationFilter[] { } });
}

app.UseCors("AllowLocalhost3000");

app.UseAuthentication();
app.UseAuthorization();

await app.RunJasperFxCommands(args);
