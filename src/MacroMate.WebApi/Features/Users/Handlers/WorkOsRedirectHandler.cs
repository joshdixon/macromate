using Carter;

using MacroMate.WebApi.Features.Users.Options;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

using WorkOS;

namespace MacroMate.WebApi.Features.Users.Handlers;

internal record WorkOsRedirectRequest(
    string ReturnPath,
    ProviderType Provider
);

internal record WorkOsRedirectResponse(
    string RedirectUrl
);

internal class WorkOsRedirectHandler : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) => app
        .MapPost("/auth/workos/redirect",
            async Task<Results<Ok<WorkOsRedirectResponse>, BadRequest<string>>> (
                WorkOsRedirectRequest request,
                HttpContext context,
                IOptions<WorkOsSettings> workOsOptions,
                SSOService sso) =>
            {
                string state = Guid.NewGuid().ToString("N");

                var opts = new GetAuthorizationURLOptions
                {
                    ClientId = workOsOptions.Value.ClientId,
                    RedirectURI = $"{request.ReturnPath}",
                    State = state,
                    Provider = request.Provider
                };

                string? url = sso.GetAuthorizationURL(opts);

                context.Response.Cookies.Append(
                    "wos_state",
                    state,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(10)
                    });

                return TypedResults.Ok(new WorkOsRedirectResponse(url ?? throw new InvalidOperationException("Failed to generate redirect URL")));
            })
        .AllowAnonymous()
        .WithName("WorkOsRedirect");
}
