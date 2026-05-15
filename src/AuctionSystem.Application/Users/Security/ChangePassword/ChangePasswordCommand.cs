using MediatR;

namespace AuctionSystem.Application.Users.Security.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Unit>;
