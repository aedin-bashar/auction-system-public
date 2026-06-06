namespace AuctionSystem.Application.Authentication.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword);
