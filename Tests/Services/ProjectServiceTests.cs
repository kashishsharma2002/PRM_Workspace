using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.DTOs.Projects;
using Server.Models.DTOs.Users;
using Server.Models.Entities;

namespace Tests;

public class ProjectServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ProjectService _projectService;
    private readonly UserService _userService;

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();

        var userRepo = new UserRepository(_context);
        var projectRepo = new ProjectRepository(_context);
        var milestoneRepo = new MilestoneRepository(_context);

        var auditService = TestServiceFactory.CreateAuditService(_context);
        var roleRepo = TestServiceFactory.CreateRoleRepository(_context);
        _userService = new UserService(
            _context,
            userRepo,
            new EmployeeRepository(_context),
            roleRepo,
            auditService,
            TestServiceFactory.CreateLogger<UserService>());
        _projectService = new ProjectService(
            projectRepo,
            milestoneRepo,
            userRepo,
            roleRepo,
            new AllocationRepository(_context),
            new EmployeeRepository(_context),
            new TimesheetRepository(_context),
            new SystemConfigRepository(_context),
            TestServiceFactory.CreateHealthThresholdProvider(_context),
            auditService,
            TestServiceFactory.CreateLogger<ProjectService>());
    }

    private async Task<long> CreateManagerAsync()
    {
        var result = await _userService.CreateUserAccountAsync(1, new CreateUserRequestDto
        {
            FullName = "Test Manager",
            Email = "mgr@techserve.com",
            Username = "test.mgr",
            TemporaryPassword = "Welcome1",
            Role = "MANAGER"
        });
        return result.UserId;
    }

    [Fact]
    public async Task CreateProjectAsync_InvalidDateRange_ThrowsValidation()
    {
        var managerId = await CreateManagerAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _projectService.CreateProjectAsync(1, new CreateProjectRequestDto
            {
                ProjectName = "Test Project",
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 5, 1),
                ProjectStatus = "ACTIVE",
                ManagerUserId = managerId,
                TotalStoryPoints = 100
            }));
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesWithProjectCode()
    {
        var managerId = await CreateManagerAsync();

        var result = await _projectService.CreateProjectAsync(1, new CreateProjectRequestDto
        {
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            ManagerUserId = managerId,
            TotalStoryPoints = 120
        });

        Assert.True(result.ProjectId > 0);
        Assert.Equal($"PRJ-{result.ProjectId:D6}", result.ProjectCode);
    }

    [Fact]
    public async Task AddMilestoneAsync_DueDateOutsideRange_ThrowsValidation()
    {
        var managerId = await CreateManagerAsync();
        var project = await _projectService.CreateProjectAsync(1, new CreateProjectRequestDto
        {
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            ManagerUserId = managerId,
            TotalStoryPoints = 120
        });

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _projectService.AddMilestoneAsync(project.ProjectId, new CreateMilestoneRequestDto
            {
                MilestoneTitle = "Late Milestone",
                DueDate = new DateOnly(2026, 12, 1),
                StoryPoints = 20
            }));
    }

    [Fact]
    public async Task GetAllProjectsAsync_StoryPointsDone_SumsDoneMilestones()
    {
        var managerId = await CreateManagerAsync();
        var project = await _projectService.CreateProjectAsync(1, new CreateProjectRequestDto
        {
            ProjectName = "SP Test",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ProjectStatus = "ACTIVE",
            ManagerUserId = managerId,
            TotalStoryPoints = 100
        });

        await _projectService.AddMilestoneAsync(project.ProjectId, new CreateMilestoneRequestDto
        {
            MilestoneTitle = "Done Work",
            DueDate = new DateOnly(2026, 3, 1),
            StoryPoints = 20
        });

        var milestones = await _projectService.GetMilestonesAsync(project.ProjectId);
        var doneMilestone = milestones.Milestones.First();
        await _projectService.UpdateMilestoneStatusAsync(project.ProjectId, doneMilestone.Id,
            new UpdateMilestoneStatusRequestDto { MilestoneStatus = "DONE" });

        var list = await _projectService.GetAllProjectsAsync();
        var item = list.Projects.First(p => p.Id == project.ProjectId);

        Assert.Equal(20, item.StoryPointsDone);
        Assert.Equal(100, item.TotalStoryPoints);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
