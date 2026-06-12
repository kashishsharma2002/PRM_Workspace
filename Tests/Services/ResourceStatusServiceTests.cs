using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Models.Entities;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Services.Employees;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests;

public class ResourceStatusServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<ILogger<ResourceStatusService>> _loggerMock;
    private readonly ResourceStatusService _resourceStatusService;
    private readonly long _profileId = 123;

    public ResourceStatusServiceTests()
    {
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _loggerMock = new Mock<ILogger<ResourceStatusService>>();

        _resourceStatusService = new ResourceStatusService(
            _employeeRepoMock.Object,
            _allocationRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void ResolveStatusFromUtilization_MapsBenchPartialAndAllocated()
    {
        Assert.Equal(ResourceStatusConstants.Bench, _resourceStatusService.ResolveStatusFromUtilization(0m));
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, _resourceStatusService.ResolveStatusFromUtilization(50m));
        Assert.Equal(ResourceStatusConstants.Allocated, _resourceStatusService.ResolveStatusFromUtilization(100m));
    }

    [Fact]
    public async Task ReconcileAllResourceStatusesAsync_UpdatesBenchWhenNoAllocations()
    {
        // Arrange
        var profile = new ResourceProfile { Id = _profileId, ResourceStatus = ResourceStatusConstants.Allocated };
        _employeeRepoMock.Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ResourceProfile> { profile });
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation>()); // No active allocations

        // Act
        var updated = await _resourceStatusService.ReconcileAllResourceStatusesAsync();

        // Assert
        Assert.Equal(1, updated);
        Assert.Equal(ResourceStatusConstants.Bench, profile.ResourceStatus);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyStatusFromActiveAllocationsAsync_SetsPartiallyAllocated()
    {
        // Arrange
        var profile = new ResourceProfile { Id = _profileId, ResourceStatus = ResourceStatusConstants.Bench };
        _employeeRepoMock.Setup(r => r.GetByIdAsync(_profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(_profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation> { new ProjectAllocation { ResourceProfileId = _profileId, AllocationPercentage = 50m } });

        // Act
        await _resourceStatusService.ApplyStatusFromActiveAllocationsAsync(_profileId);

        // Assert
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, profile.ResourceStatus);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
    }
}
