using Server.Common;
using Server.Common.Ai;
using Server.Common.Errors;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Models.DTOs.Ai.Context;
using Server.Services.Ai.Abstractions;

namespace Server.Services.Ai;

public class TeamBuilderResponseNormalizer : ITeamBuilderResponseNormalizer
{
    public TeamBuilderResponseDto Normalize(
        TeamBuilderResponseDto response,
        IReadOnlyList<AiSkillMatchCandidateContext> assignableCandidates,
        IReadOnlyList<AiSkillMatchCandidateContext> allCandidates)
    {
        if (response.Roles.Count == 0)
            ThrowInvalid("The AI returned no roles. Try again or switch LLM provider.");

        var assignableByName = assignableCandidates
            .GroupBy(c => c.FullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var allByName = allCandidates
            .GroupBy(c => c.FullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var assignedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in response.Roles)
        {
            if (string.Equals(role.Status, TeamBuilderConstants.StatusFilled, StringComparison.OrdinalIgnoreCase))
                NormalizeFilledRole(role, assignableByName, allByName, assignedNames);
            else if (string.Equals(role.Status, TeamBuilderConstants.StatusGap, StringComparison.OrdinalIgnoreCase))
                ValidateGapRole(role);
            else
                ThrowInvalid($"Role '{role.RoleTitle}' has an invalid status '{role.Status}'.");
        }

        return response;
    }

    private static void NormalizeFilledRole(
        TeamBuilderRoleResultDto role,
        IReadOnlyDictionary<string, AiSkillMatchCandidateContext> assignableByName,
        IReadOnlyDictionary<string, AiSkillMatchCandidateContext> allByName,
        Dictionary<string, string> assignedNames)
    {
        if (string.IsNullOrWhiteSpace(role.AssignedEmployeeName))
        {
            ConvertToGap(role, TeamBuilderConstants.GapReasonNoSkill,
                $"No employee could be assigned to '{role.RoleTitle}'.");
            return;
        }

        var employeeName = role.AssignedEmployeeName;

        if (assignedNames.TryGetValue(employeeName, out var priorRoleTitle))
        {
            ConvertToGap(role, TeamBuilderConstants.GapReasonAlreadyAssignedInTeam,
                $"{employeeName} is already assigned to '{priorRoleTitle}'. No other fully available employee matches this role. Consider hiring or training.",
                alternativeEmployeeName: employeeName);
            return;
        }

        if (!assignableByName.ContainsKey(employeeName))
        {
            ConvertToAllocatedElsewhereGap(role, employeeName, allByName);
            return;
        }

        assignedNames[employeeName] = role.RoleTitle;
    }

    private static void ConvertToAllocatedElsewhereGap(
        TeamBuilderRoleResultDto role,
        string employeeName,
        IReadOnlyDictionary<string, AiSkillMatchCandidateContext> allByName)
    {
        var availableFromDate = (string?)null;
        if (allByName.TryGetValue(employeeName, out var candidate) && candidate.ActiveAllocations.Count > 0)
        {
            availableFromDate = candidate.ActiveAllocations
                .Max(a => a.EndDate);
        }

        var message = allByName.ContainsKey(employeeName)
            ? $"{employeeName} has the required skills but is not fully available ({AllocationConstants.MaxUtilizationPercentage}% bench required)."
            : $"Assigned employee '{employeeName}' is not in the organization candidate pool.";

        ConvertToGap(role, TeamBuilderConstants.GapReasonAllocatedElsewhere, message,
            alternativeEmployeeName: employeeName,
            availableFromDate: availableFromDate);
    }

    private static void ConvertToGap(
        TeamBuilderRoleResultDto role,
        string reasonType,
        string message,
        string? alternativeEmployeeName = null,
        string? availableFromDate = null)
    {
        role.Status = TeamBuilderConstants.StatusGap;
        role.AssignedEmployeeName = null;
        role.MatchScore = null;
        role.Reason = null;
        role.Gap = new TeamBuilderGapDto
        {
            ReasonType = reasonType,
            Message = message,
            AlternativeEmployeeName = alternativeEmployeeName,
            AvailableFromDate = availableFromDate
        };
    }

    private static void ValidateGapRole(TeamBuilderRoleResultDto role)
    {
        var gap = role.Gap
            ?? throw new ValidationAppException(
                $"Role '{role.RoleTitle}' is GAP but has no gap details.",
                errorCode: ErrorCodes.LlmResponseInvalid);

        if (!IsValidGapReason(gap.ReasonType))
            ThrowInvalid($"Role '{role.RoleTitle}' has an invalid gap reason '{gap.ReasonType}'.");

        if (string.IsNullOrWhiteSpace(gap.Message))
            ThrowInvalid($"Role '{role.RoleTitle}' GAP has no message.");
    }

    private static bool IsValidGapReason(string reasonType) =>
        string.Equals(reasonType, TeamBuilderConstants.GapReasonNoSkill, StringComparison.OrdinalIgnoreCase)
        || string.Equals(reasonType, TeamBuilderConstants.GapReasonAllocatedElsewhere, StringComparison.OrdinalIgnoreCase)
        || string.Equals(reasonType, TeamBuilderConstants.GapReasonAlreadyAssignedInTeam, StringComparison.OrdinalIgnoreCase);

    private static void ThrowInvalid(string message) =>
        throw new ValidationAppException(message, errorCode: ErrorCodes.LlmResponseInvalid);
}
