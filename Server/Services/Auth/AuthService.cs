using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Auth;
using Server.Repositories.Roles;

namespace Server.Services.Auth;

public class AuthService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IJwtTokenService jwtTokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationAppException("Username and password are required.");

        var user = await userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Failed login attempt for username {Username}", request.Username.Trim());
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for username {Username}", request.Username.Trim());
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        var role = await roleRepository.GetRoleNameForUserAsync(user.Id, cancellationToken);
        if (string.IsNullOrEmpty(role))
        {
            logger.LogWarning("Failed login attempt for user {UserId} with no role", user.Id);
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        var resourceProfile = await userRepository.GetResourceProfileByUserIdAsync(user.Id, cancellationToken);

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} logged in successfully", user.Id);
        return jwtTokenService.CreateToken(user, role, resourceProfile);
    }

    public async Task<LoginResponseDto> ChangePasswordAsync(long userId, ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            throw new ValidationAppException("Current and new passwords are required.");

        if (!PasswordValidator.IsValid(request.NewPassword, out var passwordError))
            throw new ValidationAppException(passwordError);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAppException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationAppException("Current password is incorrect.");

        if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            throw new ValidationAppException("New password must be different from the current password.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.IsTemporaryPassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var role = await roleRepository.GetRoleNameForUserAsync(user.Id, cancellationToken)
            ?? throw new UnauthorizedAppException("User role not found.");

        var resourceProfile = await userRepository.GetResourceProfileByUserIdAsync(user.Id, cancellationToken);
        return jwtTokenService.CreateToken(user, role, resourceProfile);
    }
}
