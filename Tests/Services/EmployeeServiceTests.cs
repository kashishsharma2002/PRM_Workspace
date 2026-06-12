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
using Server.Models.DTOs.Employees;
using Server.Models.Entities;
using Server.Repositories.Roles;
using Server.Repositories.Users;
using Server.Repositories.Employees;
using Server.Repositories.Allocations;
using Server.Repositories.Projects;
using Server.Repositories.Timesheets;
using Server.Services.Shared;
using Server.Services.Employees;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IDbTransactionManager> _transactionManagerMock;
    private readonly Mock<IDbTransaction> _transactionMock;
    private readonly Mock<IEmployeeRepository> _employeeRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IRoleRepository> _roleRepoMock;
    private readonly Mock<ISkillRepository> _skillRepoMock;
    private readonly Mock<IEmployeeSkillRepository> _employeeSkillRepoMock;
    private readonly Mock<IAllocationRepository> _allocationRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ITimesheetRepository> _timesheetRepoMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<EmployeeService>> _loggerMock;
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _transactionMock = new Mock<IDbTransaction>();
        _transactionManagerMock = new Mock<IDbTransactionManager>();

        _transactionManagerMock.Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);

        _employeeRepoMock = new Mock<IEmployeeRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _roleRepoMock = new Mock<IRoleRepository>();
        _skillRepoMock = new Mock<ISkillRepository>();
        _employeeSkillRepoMock = new Mock<IEmployeeSkillRepository>();
        _allocationRepoMock = new Mock<IAllocationRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _timesheetRepoMock = new Mock<ITimesheetRepository>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<EmployeeService>>();

        _employeeService = new EmployeeService(
            _transactionManagerMock.Object,
            _employeeRepoMock.Object,
            _userRepoMock.Object,
            _roleRepoMock.Object,
            _skillRepoMock.Object,
            _employeeSkillRepoMock.Object,
            _allocationRepoMock.Object,
            _projectRepoMock.Object,
            _timesheetRepoMock.Object,
            _auditServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateEmployeeAsync_UpdatesDepartmentAndDesignation()
    {
        // Arrange
        var profile = new ResourceProfile { Id = 1, UserId = 10 };
        var user = new User { Id = 10, Department = "HR", Designation = "MGR" };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _employeeService.UpdateEmployeeAsync(1, new UpdateEmployeeRequestDto
        {
            Department = "Backend",
            Designation = "Developer"
        });

        // Assert
        Assert.Equal("BACKEND", user.Department);
        Assert.Equal("DEVELOPER", user.Designation);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_EndsAllocationsAndBlocksLogin()
    {
        // Arrange
        var profile = new ResourceProfile { Id = 1, UserId = 10, ResourceStatus = "ALLOCATED" };
        var user = new User { Id = 10, IsActive = true };
        var allocations = new List<ProjectAllocation>
        {
            new() { Id = 100, ResourceProfileId = 1, AllocationPercentage = 50, AllocationStatus = "ACTIVE" }
        };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _allocationRepoMock.Setup(r => r.GetActiveByEmployeeIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allocations);

        // Act
        await _employeeService.DeactivateEmployeeAsync(999, 1);

        // Assert
        Assert.Equal(ResourceStatusConstants.Bench, profile.ResourceStatus);
        Assert.False(user.IsActive);
        Assert.Equal(AllocationStatusConstants.Ended, allocations[0].AllocationStatus);

        _allocationRepoMock.Verify(r => r.UpdateAsync(allocations[0], It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _auditServiceMock.Verify(a => a.LogDeactivateAsync(
            999,
            AuditEntityConstants.Employees,
            1,
            It.IsAny<object>(),
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddSkillAsync_DuplicateSkill_ThrowsConflict()
    {
        // Arrange
        var profile = new ResourceProfile { Id = 1, UserId = 10 };
        var skill = new Skill { Id = 5, SkillName = "Java" };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _skillRepoMock.Setup(r => r.GetByNameAsync("Java", It.IsAny<CancellationToken>()))
            .ReturnsAsync(skill);
        _employeeSkillRepoMock.Setup(r => r.ExistsAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new AddSkillRequestDto
        {
            SkillName = "Java",
            Category = "BACKEND",
            ProficiencyLevel = "INTERMEDIATE"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictAppException>(() =>
            _employeeService.AddSkillAsync(1, request));
    }

    [Fact]
    public async Task AssignManagerAsync_InvalidManager_ThrowsValidation()
    {
        // Arrange
        var profile = new ResourceProfile { Id = 1, UserId = 10 };
        var invalidManager = new User { Id = 10, IsActive = true }; // same user id or not manager role

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidManager);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _employeeService.AssignManagerAsync(1, new AssignManagerRequestDto
            {
                ManagerUserId = 10
            }));
    }

    [Fact]
    public async Task AssignManagerAsync_ValidManager_SetsManagerId()
    {
        // Arrange
        var profile = new ResourceProfile { Id = 1, UserId = 10, ManagerId = null };
        var manager = new User { Id = 20, IsActive = true };

        _employeeRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userRepoMock.Setup(r => r.GetByIdAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        _roleRepoMock.Setup(r => r.UserHasRoleAsync(20, RoleConstants.Manager, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _employeeService.AssignManagerAsync(1, new AssignManagerRequestDto
        {
            ManagerUserId = 20
        });

        // Assert
        Assert.Equal(20, profile.ManagerId);
        _employeeRepoMock.Verify(r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _employeeRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
