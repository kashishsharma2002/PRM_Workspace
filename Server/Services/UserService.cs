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

            var auditLog = new AuditLog
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
            };

            await auditLogRepository.AddAsync(auditLog, cancellationToken);
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
}
