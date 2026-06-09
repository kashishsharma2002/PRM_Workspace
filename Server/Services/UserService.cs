using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;
using Server.Models.Entities;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;

namespace Server.Services;

public class UserService(
    PrmDbContext context,
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IAuditLogRepository auditLogRepository) : IUserService
{
    public async Task<CreateUserResponseDto> CreateUserAccountAsync(
        long actorUserId,
        CreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var role = request.Role.Trim().ToUpperInvariant();

        if (await userRepository.ExistsByUsernameOrEmailAsync(username, email, cancellationToken))
            throw new ConflictAppException("Username or email already exists.");

        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new User
            {
                Username = username,
                Email = email,
                FullName = request.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword),
                Role = role,
                ForcePasswordChange = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);

            var employee = new Employee
            {
                UserId = user.Id,
                EmployeeCode = $"EMP-{user.Id:D6}",
                EmploymentStatus = "BENCH",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await employeeRepository.AddAsync(employee, cancellationToken);

            await auditLogRepository.AddAsync(new AuditLog
            {
                ActorUserId = actorUserId,
                EntityName = "USERS",
                EntityId = user.Id,
                ActionType = "CREATE",
                NewValues = JsonSerializer.Serialize(new
                {
                    user.Username,
                    user.Email,
                    user.Role,
                    employee.EmployeeCode
                }),
                CreatedAt = now
            }, cancellationToken);

            await userRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateUserResponseDto
            {
                UserId = user.Id,
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<UserListResponseDto> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var items = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            Role = u.Role,
            IsActive = u.IsActive
        }).ToList();

        return new UserListResponseDto
        {
            Users = items,
            Total = items.Count,
            ActiveCount = items.Count(u => u.IsActive),
            InactiveCount = items.Count(u => !u.IsActive)
        };
    }

    public async Task ResetPasswordAsync(
        long actorUserId,
        long userId,
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var now = DateTime.UtcNow;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewTemporaryPassword);
        user.ForcePasswordChange = true;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);

        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = "USERS",
            EntityId = user.Id,
            ActionType = "UPDATE",
            NewValues = JsonSerializer.Serialize(new { action = "RESET_PASSWORD", user.Username }),
            CreatedAt = now
        }, cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateUserAsync(
        long actorUserId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId)
            throw new ForbiddenAppException("You cannot deactivate your own account.");

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        if (!user.IsActive)
            throw new ValidationAppException("User is already inactive.");

        var now = DateTime.UtcNow;
        user.IsActive = false;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);

        var employee = await employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (employee is not null)
        {
            employee.IsActive = false;
            employee.UpdatedAt = now;
            await employeeRepository.UpdateAsync(employee, cancellationToken);
        }

        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = "USERS",
            EntityId = user.Id,
            ActionType = "DEACTIVATE",
            OldValues = JsonSerializer.Serialize(new { isActive = true }),
            NewValues = JsonSerializer.Serialize(new { isActive = false, user.Username }),
            CreatedAt = now
        }, cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateUserAsync(
        long actorUserId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        if (user.IsActive)
            throw new ValidationAppException("User is already active.");

        var now = DateTime.UtcNow;
        user.IsActive = true;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);

        var employee = await employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (employee is not null)
        {
            employee.IsActive = true;
            employee.UpdatedAt = now;
            await employeeRepository.UpdateAsync(employee, cancellationToken);
        }

        await auditLogRepository.AddAsync(new AuditLog
        {
            ActorUserId = actorUserId,
            EntityName = "USERS",
            EntityId = user.Id,
            ActionType = "UPDATE",
            OldValues = JsonSerializer.Serialize(new { isActive = false }),
            NewValues = JsonSerializer.Serialize(new { isActive = true, user.Username }),
            CreatedAt = now
        }, cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetUserOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundAppException("User not found.");
    }
}
