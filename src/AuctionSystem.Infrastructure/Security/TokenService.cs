using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Authentication.Models;
using Microsoft.Extensions.Options;

namespace AuctionSystem.Infrastructure.Security;

public sealed class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<TokenResult> CreateAccessTokenAsync(TokenRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = DateTime.UtcNow;
        var expiresUtc = nowUtc.AddMinutes(_options.AccessTokenMinutes);

        var token = CreateJwt(request, nowUtc, expiresUtc);
        return Task.FromResult(new TokenResult(token, expiresUtc));
    }

    private string CreateJwt(TokenRequest request, DateTime issuedAtUtc, DateTime expiresUtc)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
        }

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["sub"] = request.UserId.ToString(),
            ["email"] = request.Email,
            ["role"] = request.Role,
            ["iss"] = _options.Issuer,
            ["aud"] = _options.Audience,
            ["iat"] = ToUnixTimeSeconds(issuedAtUtc),
            ["exp"] = ToUnixTimeSeconds(expiresUtc),
            ["jti"] = Guid.NewGuid().ToString("N")
        };

        var headerSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var payloadSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var unsignedToken = string.Concat(headerSegment, ".", payloadSegment);
        var signature = Base64UrlEncode(Sign(unsignedToken, _options.SigningKey));

        return string.Concat(unsignedToken, ".", signature);
    }

    private static byte[] Sign(string data, string signingKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        using var hmac = new HMACSHA256(keyBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static long ToUnixTimeSeconds(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : utcDateTime.ToUniversalTime();

        return new DateTimeOffset(utc).ToUnixTimeSeconds();
    }
}
