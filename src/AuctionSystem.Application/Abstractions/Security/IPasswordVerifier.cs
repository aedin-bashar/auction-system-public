namespace AuctionSystem.Application.Abstractions.Security;

public interface IPasswordVerifier
{
    Task<bool> VerifyAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
