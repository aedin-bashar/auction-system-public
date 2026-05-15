using AuctionSystem.Application.Authentication.Models;
using MediatR;

namespace AuctionSystem.Application.Authentication.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber) : IRequest<LoginResultDto>;
