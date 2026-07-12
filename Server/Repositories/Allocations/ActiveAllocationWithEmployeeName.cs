namespace Server.Repositories.Allocations;

public record ActiveAllocationWithEmployeeName(
    string EmployeeName,
    decimal AllocationPercentage,
    DateOnly AllocationStartDate,
    DateOnly AllocationEndDate);
