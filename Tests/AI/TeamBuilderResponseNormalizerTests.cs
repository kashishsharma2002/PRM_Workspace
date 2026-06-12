using Server.Common.Ai;
using Server.Exceptions;
using Server.Models.DTOs.Ai;
using Server.Services.Ai;
using Server.Services.Ai.Models;

namespace Tests;

public class TeamBuilderResponseNormalizerTests
{
    private readonly TeamBuilderResponseNormalizer _normalizer = new();

    [Fact]
    public void Normalize_Throws_WhenNoRolesReturned()
    {
        var response = new TeamBuilderResponseDto { Roles = [] };

        Assert.Throws<ValidationAppException>(() =>
            _normalizer.Normalize(response, [], []));
    }

    [Fact]
    public void Normalize_ConvertsDuplicateFilledToGap()
    {
        var response = new TeamBuilderResponseDto
        {
            Roles =
            [
                FilledRole("Senior React Developer (1)", "Aarav Patel"),
                FilledRole("Senior React Developer (2)", "Aarav Patel")
            ]
        };

        var result = _normalizer.Normalize(response, Assignable("Aarav Patel"), All("Aarav Patel"));

        Assert.Equal(TeamBuilderConstants.StatusFilled, result.Roles[0].Status);
        Assert.Equal(TeamBuilderConstants.StatusGap, result.Roles[1].Status);
        Assert.Equal(TeamBuilderConstants.GapReasonAlreadyAssignedInTeam, result.Roles[1].Gap?.ReasonType);
        Assert.Contains("Senior React Developer (1)", result.Roles[1].Gap?.Message);
    }

    [Fact]
    public void Normalize_ConvertsNotAssignableFilledToAllocatedElsewhereGap()
    {
        var response = new TeamBuilderResponseDto
        {
            Roles = [FilledRole("DevOps Engineer", "Ravi Kumar")]
        };

        var allocatedCandidate = new AiSkillMatchCandidateContext
        {
            FullName = "Ravi Kumar",
            RemainingCapacityPercentage = 50,
            ActiveAllocations =
            [
                new AiAllocationContext
                {
                    EndDate = "2026-09-30",
                    AllocationPercentage = 50
                }
            ]
        };

        var result = _normalizer.Normalize(response, [], [allocatedCandidate]);

        Assert.Equal(TeamBuilderConstants.StatusGap, result.Roles[0].Status);
        Assert.Equal(TeamBuilderConstants.GapReasonAllocatedElsewhere, result.Roles[0].Gap?.ReasonType);
        Assert.Equal("Ravi Kumar", result.Roles[0].Gap?.AlternativeEmployeeName);
        Assert.Equal("2026-09-30", result.Roles[0].Gap?.AvailableFromDate);
    }

    [Fact]
    public void Normalize_Passes_WhenFilledAndGapRolesValid()
    {
        var response = new TeamBuilderResponseDto
        {
            Roles =
            [
                FilledRole("Senior Java Developer", "Anil Mehta"),
                new TeamBuilderRoleResultDto
                {
                    RoleTitle = "QA Tester",
                    Status = TeamBuilderConstants.StatusGap,
                    RequiredSkills = [new TeamBuilderSkillRequirementDto { SkillName = "Selenium", MinProficiency = "BEGINNER" }],
                    Gap = new TeamBuilderGapDto
                    {
                        ReasonType = TeamBuilderConstants.GapReasonNoSkill,
                        Message = "No employee has Selenium skills."
                    }
                }
            ]
        };

        var result = _normalizer.Normalize(response, Assignable("Anil Mehta"), All("Anil Mehta"));

        Assert.Equal(TeamBuilderConstants.StatusFilled, result.Roles[0].Status);
        Assert.Equal(TeamBuilderConstants.StatusGap, result.Roles[1].Status);
    }

    [Fact]
    public void Normalize_HandlesDuplicateCandidateNamesInSeedData()
    {
        var response = new TeamBuilderResponseDto
        {
            Roles = [FilledRole("Senior Java Developer", "Anil Mehta")]
        };

        var duplicateCandidates = new List<AiSkillMatchCandidateContext>
        {
            new() { FullName = "Anil Mehta", RemainingCapacityPercentage = 100 },
            new() { FullName = "Anil Mehta", RemainingCapacityPercentage = 100 }
        };

        var result = _normalizer.Normalize(response, duplicateCandidates, duplicateCandidates);

        Assert.Equal(TeamBuilderConstants.StatusFilled, result.Roles[0].Status);
        Assert.Equal("Anil Mehta", result.Roles[0].AssignedEmployeeName);
    }

    [Fact]
    public void Normalize_Throws_WhenGapHasInvalidReasonType()
    {
        var response = new TeamBuilderResponseDto
        {
            Roles =
            [
                new TeamBuilderRoleResultDto
                {
                    RoleTitle = "QA Tester",
                    Status = TeamBuilderConstants.StatusGap,
                    Gap = new TeamBuilderGapDto
                    {
                        ReasonType = "UNKNOWN",
                        Message = "Some message"
                    }
                }
            ]
        };

        Assert.Throws<ValidationAppException>(() =>
            _normalizer.Normalize(response, [], []));
    }

    private static TeamBuilderRoleResultDto FilledRole(string title, string employeeName) =>
        new()
        {
            RoleTitle = title,
            Status = TeamBuilderConstants.StatusFilled,
            AssignedEmployeeName = employeeName,
            MatchScore = 90,
            Reason = "Good match.",
            RequiredSkills = [new TeamBuilderSkillRequirementDto { SkillName = "React", MinProficiency = "ADVANCED" }]
        };

    private static List<AiSkillMatchCandidateContext> Assignable(string fullName) =>
        [new AiSkillMatchCandidateContext { FullName = fullName, RemainingCapacityPercentage = 100 }];

    private static List<AiSkillMatchCandidateContext> All(string fullName) =>
        [new AiSkillMatchCandidateContext { FullName = fullName, RemainingCapacityPercentage = 100 }];
}
