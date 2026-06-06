using System.Security.Cryptography;
using System.Text;
using AuctionSystem.Application.Abstractions.Email;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;

namespace AuctionSystem.Application.Authentication.ForgotPassword;

public sealed class ForgotPasswordHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository tokenRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim(), cancellationToken);

        // Always return success to prevent user enumeration.
        if (user is null) return;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var resetToken = new PasswordResetToken(
            user.Id,
            tokenHash,
            DateTime.UtcNow.AddHours(1));

        await tokenRepository.AddAsync(resetToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{command.ResetBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var html = $"""
            <p>Hi,</p>
            <p>You requested to reset your Auction System password. Click the link below to choose a new password. The link expires in 1 hour.</p>
            <p><a href="{resetUrl}">Reset my password</a></p>
            <p>If you did not request this, you can safely ignore this email.</p>
            """;

        await emailSender.SendAsync(command.Email, "Reset your password", html, cancellationToken);
    }
}
