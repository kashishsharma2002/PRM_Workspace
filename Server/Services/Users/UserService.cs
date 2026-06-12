using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Common;
using Server.Common.Audit;
using Server.Common.Errors;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Users;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Services.Shared;

namespace Server.Services.Users;

public class UserService(
    PrmDbContext context,
    IUserRepository userRepository,
    IEmployeeRepository employeeRepository,
    IRoleRepository roleRepository,
    IAuditService auditService,
    ILogger<UserService> logger) : IUserService
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

        var roleEntity = await roleRepository.GetByNameAsync(role, cancellationToken)
            ?? throw new ValidationAppException("Invalid role.");

        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var (department, designation) = ResolveDepartmentAndDesignation(role, request);

            var user = new User
            {
                Username = username,
                Email = email,
                FullName = request.FullName.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.TemporaryPassword),
                Department = department,
                Designation = designation,
                IsTemporaryPassword = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await userRepository.AddAsync(user, cancellationToken);
            await userRepository.SaveChangesAsync(cancellationToken);

            await roleRepository.AssignRoleAsync(user.Id, roleEntity.Id, actorUserId, cancellationToken);
            await roleRepository.SaveChangesAsync(cancellationToken);

            long? resourceProfileId = null;
            if (role is RoleConstants.Employee)
            {
                var profile = new ResourceProfile
                {
                    UserId = user.Id,
                    ResourceStatus = ResourceStatusConstants.Bench,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await employeeRepository.AddAsync(profile, cancellationToken);
                await employeeRepository.SaveChangesAsync(cancellationToken);
                resourceProfileId = profile.Id;
            }

            await auditService.LogCreateAsync(
                actorUserId,
                AuditEntityConstants.Users,
                user.Id,
                new { user.Username, user.Email, Role = role, ResourceProfileId = resourceProfileId },
                cancellationToken);

            await userRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "User created. {EntityName} {EntityId} by {ActorUserId}",
                AuditEntityConstants.Users, user.Id, actorUserId);

            return new CreateUserResponseDto
            {
                UserId = user.Id,
                EmployeeId = resourceProfileId ?? 0,
                EmployeeCode = resourceProfileId.HasValue ? $"EMP-{user.Id:D6}" : string.Empty
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
        var items = new List<UserListItemDto>();

        foreach (var user in users)
        {
            var role = await roleRepository.GetRoleNameForUserAsync(user.Id, cancellationToken) ?? string.Empty;
            items.Add(new UserListItemDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = role,
                IsActive = user.IsActive
            });
        }

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
        var newPassword = request.NewTemporaryPassword.Trim();

        if (!PasswordValidator.IsValid(newPassword, out var passwordError))
            throw new ValidationAppException(passwordError);

        var now = DateTime.UtcNow;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.IsTemporaryPassword = true;
        user.UpdatedAt = now;

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.Users,
            user.Id,
            null,
            new { action = "RESET_PASSWORD", user.Username },
            cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Password reset. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Users, user.Id, actorUserId);
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

        var profile = await employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.ResourceStatus = ResourceStatusConstants.Bench;
            profile.UpdatedAt = now;
            await employeeRepository.UpdateAsync(profile, cancellationToken);
        }

        await auditService.LogDeactivateAsync(
            actorUserId,
            AuditEntityConstants.Users,
            user.Id,
            new { isActive = true },
            new { isActive = false, user.Username },
            cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User deactivated. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Users, user.Id, actorUserId);
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

        var profile = await employeeRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (profile is not null)
        {
            profile.UpdatedAt = now;
            await employeeRepository.UpdateAsync(profile, cancellationToken);
        }

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.Users,
            user.Id,
            new { isActive = false },
            new { isActive = true, user.Username },
            cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User reactivated. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Users, user.Id, actorUserId);
    }

    public async Task UpdateUserAsync(
        long actorUserId,
        long userId,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        var existingWithEmail = await userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingWithEmail is not null && existingWithEmail.Id != userId)
            throw new ConflictAppException("Email is already in use.");

        var now = DateTime.UtcNow;
        var oldValues = new { user.FullName, user.Email };

        user.FullName = fullName;
        user.Email = email;
        user.UpdatedAt = now;

        await userRepository.UpdateAsync(user, cancellationToken);

        await auditService.LogUpdateAsync(
            actorUserId,
            AuditEntityConstants.Users,
            user.Id,
            oldValues,
            new { user.FullName, user.Email },
            cancellationToken);

        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User updated. {EntityName} {EntityId} by {ActorUserId}",
            AuditEntityConstants.Users, user.Id, actorUserId);
    }

    public async Task UpdateUserRoleAsync(
        long actorUserId,
        long userId,
        UpdateUserRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId)
            throw new ForbiddenAppException("You cannot change your own role.");

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
        var newRole = request.Role.Trim().ToUpperInvariant();
        var roleEntity = await roleRepository.GetByNameAsync(newRole, cancellationToken)
            ?? throw new ValidationAppException("Invalid role.");

        var currentRole = await roleRepository.GetRoleNameForUserAsync(userId, cancellationToken);
        if (currentRole == newRole)
            throw new ValidationAppException("User already has this role.");

        var now = DateTime.UtcNow;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await roleRepository.ReplaceUserRoleAsync(userId, roleEntity.Id, actorUserId, cancellationToken);
            await roleRepository.SaveChangesAsync(cancellationToken);

            if (newRole == RoleConstants.Employee)
            {
                var profile = await employeeRepository.GetByUserIdAsync(userId, cancellationToken);
                if (profile is null)
                {
                    await employeeRepository.AddAsync(new ResourceProfile
                    {
                        UserId = userId,
                        ResourceStatus = ResourceStatusConstants.Bench,
                        CreatedAt = now,
                        UpdatedAt = now
                    }, cancellationToken);
                    await employeeRepository.SaveChangesAsync(cancellationToken);
                }
            }

            await auditService.LogUpdateAsync(
                actorUserId,
                AuditEntityConstants.Users,
                user.Id,
                new { role = currentRole },
                new { role = newRole },
                cancellationToken);

            await userRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "User role updated. {EntityName} {EntityId} by {ActorUserId} to {NewRole}",
                AuditEntityConstants.Users, user.Id, actorUserId, newRole);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<User> GetUserOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        return await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundAppException("User not found.", ErrorCodes.UserNotFound);
    }

    private static (string? Department, string? Designation) ResolveDepartmentAndDesignation(
        string role,
        CreateUserRequestDto request)
    {
        var department = NormalizeOptional(request.Department);
        var designation = NormalizeOptional(request.Designation);

        if (role == RoleConstants.Admin)
        {
            department ??= DepartmentConstants.HrOps;
            designation ??= DesignationConstants.SystemAdministrator;
        }

        return (department, designation);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
