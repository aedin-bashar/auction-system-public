using MediatR;
using AuctionSystem.Application.Authentication.Models;

namespace AuctionSystem.Application.Authentication.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResultDto>;
