using AuctionSystem.Application.Authentication.Models;

namespace AuctionSystem.Application.Abstractions.Security;

public interface ITokenService
{
    Task<TokenResult> CreateAccessTokenAsync(TokenRequest request, CancellationToken cancellationToken = default);
}
