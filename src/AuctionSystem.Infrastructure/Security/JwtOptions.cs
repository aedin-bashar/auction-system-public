namespace AuctionSystem.Infrastructure.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "AuctionSystem";
    public string Audience { get; set; } = "AuctionSystem.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}
