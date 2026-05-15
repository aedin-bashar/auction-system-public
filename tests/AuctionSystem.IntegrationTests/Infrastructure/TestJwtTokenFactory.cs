using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuctionSystem.IntegrationTests.Infrastructure;

internal static class TestJwtTokenFactory
{
    private const string Issuer = "AuctionSystem";
    private const string Audience = "AuctionSystem.Client";
    private const string SigningKey = "ReplaceThisWithASecureAtLeast32CharSecretKey123!";

    public static string Create(
        Guid userId,
        string email,
        string role,
        DateTime? notBefore = null,
        DateTime? expires = null,
        string? signingKey = null,
        string? issuer = null,
        string? audience = null)
    {
        var now = DateTime.UtcNow;
        var effectiveNotBefore = notBefore ?? now;
        var effectiveExpires = expires ?? now.AddMinutes(60);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("role", role),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer,
            audience: audience ?? Audience,
            claims: claims,
            notBefore: effectiveNotBefore,
            expires: effectiveExpires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
