using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Server.Common;
using Server.Common.Allocations;
using Server.Common.Audit;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Allocations;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Services.Shared;
using Server.Services.Employees;
using Server.Services.Allocations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class AllocationServiceTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<IResourceStatusService> _resourceStatusServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<AllocationService>> _loggerMock;
    private readonly AllocationService _allocationService;

    private readonly long _managerUserId = 1;
    private readonly long _employeeId = 10;
    private readonly long _projectId = 100;
    private readonly string _employeeFullName = "Employee A";
    private readonly string _projectName = "Project A";

    public AllocationServiceTests()
    {
        _transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();

        _transactionManagerMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _allocationRepoMock = new Mock<IAllocationRepository>();
        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _resourceStatusServiceMock = new Mock<IResourceStatusService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<AllocationService>>();

        _allocationService = new AllocationService(
            _transactionManagerMock.Object,
            _allocationRepoMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _projectRepoMock.Object,
            _resourceStatusServiceMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAllocationsAsync_ReturnsJoinedNames()
    {
        // Arrange
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 50, ResourceProfileId = _employeeId, ProjectId = _projectId, AllocationPercentage = 50, AllocationStatus = "ACTIVE" }
        };

        _allocationRepoMock.Setup(r => r.GetAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);
        _employeeRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, ResourceProfile>
            {
                [_employeeId] = new ResourceProfile { Id = _employeeId, UserId = 101 }
            });
        _userRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, User>
            {
                [101] = new User { Id = 101, FullName = _employeeFullName }
            });
        _projectRepoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, Project>
            {
                [_projectId] = new Project { Id = _projectId, ProjectName = _projectName }
            });

        // Act
        var result = await _allocationService.GetAllAllocationsAsync(null, null, null);

        // Assert
        Assert.Single(result.Allocations);
        Assert.Equal(_employeeFullName, result.Allocations[0].EmployeeName);
        Assert.Equal(_projectName, result.Allocations[0].ProjectName);
        Assert.Equal(1, result.TotalActiveCount);
    }

    [Fact]
    public async Task CreateAllocationAsync_Success_SetsEmployeeAllocated()
    {
        // Arrange
        var benchEmployee = new ResourceProfile { Id = _employeeId, UserId = 101, ManagerId = _managerUserId, ResourceStatus = ResourceStatusConstants.Bench };
        var user = new User { Id = 101, FullName = "Bench Employee", IsActive = true };
        var project = new Project { Id = _projectId, ManagerUserId = _managerUserId, ProjectStatus = "ACTIVE", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(benchEmployee);
        _userRepoMock.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation>());

        _allocationRepoMock.Setup(r => r.AddAsync(It.IsAny<ProjectAllocation>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectAllocation, CancellationToken>((a, c) => a.Id = 99);

        // Act
        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 30,
            AllocationStartDate = new DateOnly(2026, 7, 1),
            AllocationEndDate = new DateOnly(2026, 9, 30)
        };

        // We simulate ApplyStatusFromActiveAllocationsAsync modifying status to PartiallyAllocated
        _resourceStatusServiceMock.Setup(s => s.ApplyStatusFromActiveAllocationsAsync(_employeeId, It.IsAny<CancellationToken>()))
            .Callback(() => benchEmployee.ResourceStatus = ResourceStatusConstants.PartiallyAllocated)
            .Returns(Task.CompletedTask);

        var result = await _allocationService.CreateAllocationAsync(_managerUserId, request);

        // Assert
        Assert.Equal(99, result.AllocationId);
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, result.EmploymentStatus);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAllocationAsync_OverAllocation_Throws()
    {
        // Arrange
        var profile = new ResourceProfile { Id = _employeeId, UserId = 101, ManagerId = _managerUserId };
        var user = new User { Id = 101, FullName = "Employee A", IsActive = true };
        var project = new Project { Id = _projectId, ManagerUserId = _managerUserId, ProjectStatus = "ACTIVE", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31) };
        var existingAllocations = new List<ProjectAllocation>
        {
            new() { Id = 50, ResourceProfileId = _employeeId, AllocationPercentage = 50, AllocationStartDate = new DateOnly(2026, 3, 1), AllocationEndDate = new DateOnly(2026, 6, 30), AllocationStatus = "ACTIVE" }
        };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAllocations);

        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 60,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30)
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _allocationService.CreateAllocationAsync(_managerUserId, request));

        Assert.Contains("110%", ex.Message);
        Assert.Contains("Maximum is 100%", ex.Message);
    }

    [Fact]
    public async Task CreateAllocationAsync_NotManagerTeam_Throws()
    {
        // Arrange
        var profile = new ResourceProfile { Id = _employeeId, UserId = 101, ManagerId = 999 }; // Different manager ID
        var user = new User { Id = 101, FullName = "Employee A", IsActive = true };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 20,
            AllocationStartDate = new DateOnly(2026, 7, 1),
            AllocationEndDate = new DateOnly(2026, 8, 31)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _allocationService.CreateAllocationAsync(_managerUserId, request));
    }

    [Fact]
    public async Task EndAllocationAsync_SetsBenchWhenNoOtherActive()
    {
        // Arrange
        var allocation = new ProjectAllocation { Id = 50, ResourceProfileId = _employeeId, ProjectId = _projectId, AllocationPercentage = 50, AllocationStatus = "ACTIVE" };
        var profile = new ResourceProfile { Id = _employeeId, UserId = 101, ManagerId = _managerUserId, ResourceStatus = ResourceStatusConstants.Allocated };
        var project = new Project { Id = _projectId, ManagerUserId = _managerUserId };

        _allocationRepoMock.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProjectAllocation> { allocation });

        _resourceStatusServiceMock.Setup(s => s.ApplyStatusFromActiveAllocationsAsync(_employeeId, It.IsAny<CancellationToken>()))
            .Callback(() => profile.ResourceStatus = ResourceStatusConstants.Bench)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _allocationService.EndAllocationAsync(_managerUserId, 50);

        // Assert
        Assert.Equal(AllocationStatusConstants.Ended, allocation.AllocationStatus);
        Assert.Equal(ResourceStatusConstants.Bench, profile.ResourceStatus);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EndAllocationAsync_NotProjectOwner_Throws()
    {
        // Arrange
        var allocation = new ProjectAllocation { Id = 50, ResourceProfileId = _employeeId, ProjectId = _projectId, AllocationPercentage = 50, AllocationStatus = "ACTIVE" };
        var project = new Project { Id = _projectId, ManagerUserId = 999 }; // Different manager

        _allocationRepoMock.Setup(r => r.GetByIdAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocation);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _allocationService.EndAllocationAsync(_managerUserId, 50));
    }

    [Fact]
    public async Task CreateAllocationAsync_CompletedProject_ThrowsValidation()
    {
        // Arrange
        var profile = new ResourceProfile { Id = _employeeId, UserId = 101, ManagerId = _managerUserId, ResourceStatus = ResourceStatusConstants.Bench };
        var user = new User { Id = 101, FullName = "Employee Completed", IsActive = true };
        var completedProject = new Project { Id = _projectId, ManagerUserId = _managerUserId, ProjectStatus = "COMPLETED" };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(_employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _projectRepoMock.Setup(r => r.GetByIdAsync(_projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedProject);

        var request = new CreateAllocationRequestDto
        {
            EmployeeId = _employeeId,
            ProjectId = _projectId,
            AllocationPercentage = 50,
            AllocationStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AllocationEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationAppException>(
            () => _allocationService.CreateAllocationAsync(_managerUserId, request));

        Assert.Contains("ACTIVE or PLANNED", ex.Message);
    }
}
