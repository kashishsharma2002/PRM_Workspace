using Server.Common;
using Server.Exceptions;
using Server.Models.DTOs.Auth;

namespace Server.Services.Auth;

public class AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ValidationAppException("Username and password are required.");

        var user = await userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAppException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Invalid username or password.");

        var employee = await userRepository.GetEmployeeByUserIdAsync(user.Id, cancellationToken);
        if (employee is not null && !employee.IsActive)
            throw new UnauthorizedAppException("Invalid username or password.");

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return jwtTokenService.CreateToken(user, employee);
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
        user.ForcePasswordChange = false;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        var employee = await userRepository.GetEmployeeByUserIdAsync(user.Id, cancellationToken);
        return jwtTokenService.CreateToken(user, employee);
    }
}
