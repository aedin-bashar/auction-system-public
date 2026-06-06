namespace AuctionSystem.Application.Authentication.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email, string ResetBaseUrl);
