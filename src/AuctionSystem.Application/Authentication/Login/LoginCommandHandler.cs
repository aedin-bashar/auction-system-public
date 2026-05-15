using AuctionSystem.Application.Abstractions.Security;
using AuctionSystem.Application.Authentication.Models;
using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Authentication.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordVerifier _passwordVerifier;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordVerifier passwordVerifier,
        ITokenService tokenService)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _passwordVerifier = passwordVerifier ?? throw new ArgumentNullException(nameof(passwordVerifier));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var passwordOk = await _passwordVerifier.VerifyAsync(user.Id, request.Password, cancellationToken);
        if (!passwordOk)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

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
