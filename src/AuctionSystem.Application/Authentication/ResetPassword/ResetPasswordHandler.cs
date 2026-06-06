using System.Security.Cryptography;
using System.Text;
using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Domain.Abstractions;

namespace AuctionSystem.Application.Authentication.ResetPassword;

public sealed class ResetPasswordHandler(
    IPasswordResetTokenRepository tokenRepository,
    IPasswordStore passwordStore,
    IUnitOfWork unitOfWork)
{
    public async Task<ResetPasswordResult> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(command.Token)));

        var record = await tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (record is null)
            return new ResetPasswordResult(false, "The reset link is invalid or has expired.");

        if (record.ExpiresAtUtc < DateTime.UtcNow)
            return new ResetPasswordResult(false, "The reset link has expired. Request a new one.");

        if (record.UsedAtUtc is not null)
            return new ResetPasswordResult(false, "This reset link has already been used.");

        await passwordStore.SetPasswordAsync(record.UserId, command.NewPassword, cancellationToken);

        record.UsedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResult(true);
    }
}
