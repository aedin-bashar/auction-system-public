namespace AuctionSystem.Application.Abstractions.Security;

public interface IPasswordStore
{
    Task SetPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
