using Server.Models.DTOs.Allocations;
using Server.Repositories.Interfaces;
using Server.Services.Interfaces;

namespace Server.Services;

public class AllocationService(
    IAllocationRepository allocationRepository,
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IProjectRepository projectRepository) : IAllocationService
{
    public async Task<AllocationListResponseDto> GetAllAllocationsAsync(
        long? employeeId,
        long? projectId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var allocations = await allocationRepository.GetAllAsync(employeeId, projectId, status, cancellationToken);
        var items = new List<AllocationListItemDto>();

        foreach (var allocation in allocations)
        {
            var employee = await employeeRepository.GetByIdAsync(allocation.EmployeeId, cancellationToken);
            var employeeName = string.Empty;
            if (employee is not null)
            {
                var user = await userRepository.GetByIdAsync(employee.UserId, cancellationToken);
                employeeName = user?.FullName ?? string.Empty;
            }

            var project = await projectRepository.GetByIdAsync(allocation.ProjectId, cancellationToken);

            items.Add(new AllocationListItemDto
            {
                Id = allocation.Id,
                EmployeeName = employeeName,
                ProjectName = project?.ProjectName ?? "Unknown",
                AllocationPercentage = allocation.AllocationPercentage,
                AllocationStartDate = allocation.AllocationStartDate,
                AllocationEndDate = allocation.AllocationEndDate,
                AllocationStatus = allocation.AllocationStatus
            });
        }

        return new AllocationListResponseDto
        {
            Allocations = items,
            TotalActiveCount = items.Count(i => i.AllocationStatus == "ACTIVE")
        };
    }
}
