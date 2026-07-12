namespace Server.Repositories.Allocations;

public record ActiveAllocationWithProjectName(
    long ProjectId,
    long ResourceProfileId,
    decimal AllocationPercentage,
    DateOnly AllocationStartDate,
    DateOnly AllocationEndDate,
    string ProjectName);
