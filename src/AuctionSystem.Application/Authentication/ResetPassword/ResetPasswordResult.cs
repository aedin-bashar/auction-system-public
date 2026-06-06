namespace AuctionSystem.Application.Authentication.ResetPassword;

public sealed record ResetPasswordResult(bool Succeeded, string? Error = null);
