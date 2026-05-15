using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AuctionSystem.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        if (principal is null) throw new ArgumentNullException(nameof(principal));

        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Authenticated user id claim is missing.");
        }

        return userId;
    }
}
