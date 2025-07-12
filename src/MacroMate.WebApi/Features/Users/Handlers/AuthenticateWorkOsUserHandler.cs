using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

using Carter;

using MacroMate.WebApi.Features.Users.Aggregates;
using MacroMate.WebApi.Features.Users.Events;
using MacroMate.WebApi.Features.Users.Options;
using MacroMate.WebApi.Features.Users.Projections;

using Marten;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MacroMate.WebApi.Features.Users.Handlers;

internal record AuthenticateWorkOsUserRequest(string Code, string State, string RedirectUri);

internal record AuthenticateUserResult(string AccessToken, string RefreshToken);

internal class AuthenticateWorkOsUser : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app) => app
        .MapPost("/auth/workos",
            async Task<Results<Ok<AuthenticateUserResult>, BadRequest<string>>> (
                [FromBody] AuthenticateWorkOsUserRequest request,
                HttpContext context,
                IDocumentSession session,
                IOptions<WorkOsSettings> workOsOptions,
                IOptions<JwtOptions> jwtOptions,
                HttpClient httpClient,
                JwtSecurityTokenHandler tokenHandler) =>
            {
                if (context.Request.Cookies["wos_state"] != request.State)
                {
                    return TypedResults.BadRequest("State mismatch");
                }

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {workOsOptions.Value.ApiKey}");
                HttpResponseMessage authResponse = await httpClient.PostAsJsonAsync("https://api.workos.com/user_management/authenticate",
                    new
                    {
                        client_id = workOsOptions.Value.ClientId,
                        client_secret = workOsOptions.Value.ApiKey,
                        grant_type = "authorization_code",
                        code = request.Code
                    });

                if (!authResponse.IsSuccessStatusCode)
                {
                    return TypedResults.BadRequest("Failed to authenticate");
                }

                var auth = await authResponse.Content.ReadFromJsonAsync<WorkOsAuthenticateResponse>();
                WorkOsUser profile = auth.User;

                UserView? existingUser = await session
                    .Query<UserView>()
                    .Where(x => x.WorkOsUserId == profile.Id)
                    .SingleOrDefaultAsync();

                var userId = existingUser?.Id ?? Guid.NewGuid();
                if (existingUser is null)
                {
                    session.Events.StartStream<User>(userId,
                        new UserCreated(
                            userId, 
                            profile.FirstName.ToLower(), 
                            profile.FirstName,
                            profile.LastName,
                            profile.Email,
                            profile.Id));
                    
                    await session.SaveChangesAsync();
                }
                
                AuthenticateUserResult tokens = GenerateTokens(userId,
                    profile.Email,
                    profile.FirstName.ToLower(),
                    tokenHandler,
                    jwtOptions);

                return TypedResults.Ok(tokens);
            })
        .AllowAnonymous()
        .WithName("AuthenticateWorkOsUser");

    private static AuthenticateUserResult GenerateTokens(Guid userId,
        string email,
        string displayName,
        JwtSecurityTokenHandler tokenHandler,
        IOptions<JwtOptions> jwtOptions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.PrivateKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        DateTime expiry = DateTime.UtcNow.AddMinutes(jwtOptions.Value.TokenExpiryInMinutes);
        var token = new JwtSecurityToken(jwtOptions.Value.Issuer,
            jwtOptions.Value.Audience,
            claims,
            expires: expiry,
            signingCredentials: creds);

        string jwt = tokenHandler.WriteToken(token);
        string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new AuthenticateUserResult(jwt, refreshToken);
    }
}

file record WorkOsUser(
    [property: JsonPropertyName("object")] string ObjectType,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("first_name")]
    string FirstName,
    [property: JsonPropertyName("last_name")]
    string LastName,
    [property: JsonPropertyName("email_verified")]
    bool EmailVerified,
    [property: JsonPropertyName("profile_picture_url")]
    string? ProfilePictureUrl,
    [property: JsonPropertyName("last_sign_in_at")]
    DateTimeOffset? LastSignInAt,
    [property: JsonPropertyName("external_id")]
    string ExternalId,
    [property: JsonPropertyName("metadata")]
    Dictionary<string, string>? Metadata,
    [property: JsonPropertyName("created_at")]
    DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updated_at")]
    DateTimeOffset UpdatedAt
);

file record WorkOsAuthenticateResponse(
    [property: JsonPropertyName("user")] WorkOsUser User,
    [property: JsonPropertyName("organization_id")]
    string OrganizationId,
    [property: JsonPropertyName("authentication_method")]
    string AuthenticationMethod
);
