namespace MacroMate.WebApi.Features.Users.Options;

internal class JwtOptions
{
    public required string PrivateKey { get; set; }
    public required string Issuer { get; set; }
    public required string Audience { get; set; }
    public int TokenExpiryInMinutes { get; set; }
}
