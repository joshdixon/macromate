using System.Security.Claims;

namespace MacroMate.WebApi;

internal static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext context) => Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
    
}
