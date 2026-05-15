using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Users;
using MediatR;

namespace AuctionSystem.Application.Authentication.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, LoginResultDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordStore _passwordStore;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository users,
        IPasswordStore passwordStore,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _passwordStore = passwordStore ?? throw new ArgumentNullException(nameof(passwordStore));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<LoginResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var existingUser = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        var user = User.Register(
            request.Email,
            request.FullName,
            UserRole.Bidder,
            request.PhoneNumber);

        await _users.AddAsync(user, cancellationToken);
        await _passwordStore.SetPasswordAsync(user.Id, request.Password, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = await _tokenService.CreateAccessTokenAsync(
            new TokenRequest(user.Id, user.Email, user.Role.ToString()),
            cancellationToken);

        return new LoginResultDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            token.AccessToken,
            token.ExpiresAtUtc);
    }
}
