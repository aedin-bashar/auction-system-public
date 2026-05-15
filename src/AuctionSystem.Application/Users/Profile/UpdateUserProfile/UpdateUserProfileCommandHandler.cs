using AuctionSystem.Domain.Abstractions;
using MediatR;

namespace AuctionSystem.Application.Users.Profile.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UserProfileDto> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User was not found.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (!string.Equals(user.Email, normalizedEmail, StringComparison.Ordinal))
        {
            user.ChangeEmail(request.Email);
        }

        user.UpdateProfile(request.FullName, request.PhoneNumber);

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAtUtc,
            user.UpdatedAtUtc);
    }
}
