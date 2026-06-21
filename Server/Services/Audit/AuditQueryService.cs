using Server.Common.Audit;
using Server.Models.DTOs.Audit;
using Server.Models.Queries;
using Server.Repositories.Roles;
using Server.Repositories.Shared;
using Server.Repositories.Users;

namespace Server.Services.Audit;

public class AuditQueryService(
    IAuditLogRepository auditLogRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository) : IAuditQueryService
{
    public async Task<AuditLogListResponseDto> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;
        var normalizedQuery = new AuditLogQuery
        {
            From = query.From,
            To = query.To,
            ActorUserId = query.ActorUserId,
            EntityName = query.EntityName,
            ActionType = query.ActionType,
            Search = query.Search,
            Page = page,
            PageSize = pageSize
        };

        var (items, totalCount) = await auditLogRepository.QueryAsync(normalizedQuery, cancellationToken);
        var actorIds = items.Select(i => i.ActorUserId).Distinct().ToList();
        var actors = await userRepository.GetByIdsAsync(actorIds, cancellationToken);
        var actorRoles = await roleRepository.GetRoleNamesForUsersAsync(actorIds, cancellationToken);

        var dtos = items.Select(item =>
        {
            actors.TryGetValue(item.ActorUserId, out var actor);
            actorRoles.TryGetValue(item.ActorUserId, out var actorRole);

            var summary = !string.IsNullOrWhiteSpace(item.Summary)
                ? item.Summary
                : AuditMessageBuilder.BuildFromStoredValues(
                    item.EntityName,
                    item.ActionType,
                    item.OldValues,
                    item.NewValues);

            return new AuditLogItemDto
            {
                Id = item.Id,
                OccurredAt = item.CreatedAt,
                ActorUserId = item.ActorUserId,
                ActorName = actor?.FullName ?? $"User {item.ActorUserId}",
                ActorRole = actorRole,
                EntityName = item.EntityName,
                EntityLabel = FormatEntityLabel(item.EntityName),
                ActionType = item.ActionType,
                ActionLabel = FormatActionLabel(item.ActionType),
                Summary = summary
            };
        }).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new AuditLogListResponseDto
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static string FormatEntityLabel(string entityName) =>
        entityName switch
        {
            AuditEntityConstants.Users => "Users",
            AuditEntityConstants.Employees => "Employees",
            AuditEntityConstants.Projects => "Projects",
            AuditEntityConstants.ProjectAllocations => "Allocations",
            AuditEntityConstants.Timesheets => "Timesheets",
            AuditEntityConstants.SystemConfigurations => "System Config",
            AuditEntityConstants.ResourceProfiles => "Resources",
            AuditEntityConstants.Roles => "Roles",
            AuditEntityConstants.Auth => "Sign-in",
            _ => entityName
        };

    private static string FormatActionLabel(string actionType) =>
        actionType switch
        {
            AuditActionConstants.Create => "Created",
            AuditActionConstants.Update => "Updated",
            AuditActionConstants.Deactivate => "Deactivated",
            AuditActionConstants.End => "Ended",
            AuditActionConstants.Login => "Signed in",
            AuditActionConstants.LoginFailed => "Failed sign-in",
            _ => actionType
        };
}
