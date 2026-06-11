using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Data;
using Server.Models.Entities;
using Tests.Helpers;

namespace Tests;

public class SchedulerHealthServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ProjectService _projectService;
    private readonly long _cleanProjectId;
    private readonly long _overdueProjectId;
    private readonly long _multiFlagProjectId;

    public SchedulerHealthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        (_cleanProjectId, _overdueProjectId, _multiFlagProjectId) = SeedData();

        _projectService = CreateProjectService(new ProjectRepository(_context));
    }

    private ProjectService CreateProjectService(IProjectRepository projectRepository) =>
        new(
            projectRepository,
            new MilestoneRepository(_context),
            new UserRepository(_context),
            TestServiceFactory.CreateRoleRepository(_context),
            new AllocationRepository(_context),
            new EmployeeRepository(_context),
            new TimesheetRepository(_context),
            new SystemConfigRepository(_context),
            TestServiceFactory.CreateHealthThresholdProvider(_context),
            TestServiceFactory.CreateAuditService(_context),
            TestServiceFactory.CreateLogger<ProjectService>());

    private (long cleanId, long overdueId, long multiFlagId) SeedData()
    {
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = WeekDateHelper.GetMostRecentCompletedWeekMonday();
        var managerRole = _context.Roles.First(r => r.RoleName == Server.Common.Roles.RoleConstants.Manager);
        var employeeRole = _context.Roles.First(r => r.RoleName == Server.Common.Roles.RoleConstants.Employee);

        var manager = new User
        {
            Username = "ankit.shah",
            Email = "ankit@techserve.com",
            FullName = "Ankit Shah",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(manager);
        _context.SaveChanges();

        _context.UserRoles.Add(new UserRole
        {
            UserId = manager.Id,
            RoleId = managerRole.Id,
            AssignedAt = now
        });

        var clean = new Project
        {
            ProjectCode = "PRJ-000001",
            ProjectName = "Clean Project",
            StartDate = today.AddMonths(-2),
            EndDate = today.AddMonths(6),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = manager.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var overdue = new Project
        {
            ProjectCode = "PRJ-000002",
            ProjectName = "Overdue Project",
            StartDate = today.AddMonths(-2),
            EndDate = today.AddMonths(6),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = manager.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var multiFlag = new Project
        {
            ProjectCode = "PRJ-000003",
            ProjectName = "Multi Flag Project",
            StartDate = today.AddMonths(-2),
            EndDate = today.AddDays(10),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = manager.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Projects.AddRange(clean, overdue, multiFlag);
        _context.SaveChanges();

        _context.ProjectMilestones.AddRange(
            new ProjectMilestone
            {
                ProjectId = overdue.Id,
                MilestoneTitle = "Late API",
                DueDate = today.AddDays(-3),
                MilestoneStatus = "IN_PROGRESS",
                SortOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProjectMilestone
            {
                ProjectId = multiFlag.Id,
                MilestoneTitle = "Late Testing",
                DueDate = today.AddDays(-2),
                MilestoneStatus = "IN_PROGRESS",
                SortOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new ProjectMilestone
            {
                ProjectId = multiFlag.Id,
                MilestoneTitle = "Go Live",
                DueDate = today.AddDays(5),
                MilestoneStatus = "NOT_STARTED",
                SortOrder = 2,
                CreatedAt = now,
                UpdatedAt = now
            });
        _context.SaveChanges();

        var employeeUser = new User
        {
            Username = "ravi.kumar",
            Email = "ravi@techserve.com",
            FullName = "Ravi Kumar",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(employeeUser);
        _context.SaveChanges();

        _context.UserRoles.Add(new UserRole
        {
            UserId = employeeUser.Id,
            RoleId = employeeRole.Id,
            AssignedAt = now
        });

        var managerProfile = new ResourceProfile
        {
            UserId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Bench,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(managerProfile);
        _context.SaveChanges();

        var employee = new ResourceProfile
        {
            UserId = employeeUser.Id,
            ManagerId = manager.Id,
            ResourceStatus = ResourceStatusConstants.Allocated,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(employee);
        _context.SaveChanges();

        _context.ProjectAllocations.Add(new ProjectAllocation
        {
            ResourceProfileId = employee.Id,
            ProjectId = multiFlag.Id,
            AllocationPercentage = 100m,
            AllocationStartDate = lastWeek.AddMonths(-1),
            AllocationEndDate = today.AddMonths(3),
            AllocationStatus = "ACTIVE",
            AllocatedByUserId = manager.Id,
            CreatedAt = now,
            UpdatedAt = now
        });
        _context.SaveChanges();

        _context.SystemConfigurations.Add(new SystemConfiguration
        {
            ConfigKey = ConfigKeys.MaxWeeklyHours,
            ConfigValue = "40",
            Description = "Max weekly hours",
            UpdatedAt = now
        });
        _context.SaveChanges();

        return (clean.Id, overdue.Id, multiFlag.Id);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_NoFlags_StaysGreen()
    {
        await _projectService.EvaluateAllProjectsHealthAsync();

        var project = await _context.Projects.FindAsync(_cleanProjectId);
        Assert.NotNull(project);
        Assert.Equal("GREEN", project.HealthStatus);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_OverdueMilestone_SetsAmber()
    {
        await _projectService.EvaluateAllProjectsHealthAsync();

        var project = await _context.Projects.FindAsync(_overdueProjectId);
        Assert.NotNull(project);
        Assert.Equal("AMBER", project.HealthStatus);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_MultipleFlags_SetsRed()
    {
        await _projectService.EvaluateAllProjectsHealthAsync();

        var project = await _context.Projects.FindAsync(_multiFlagProjectId);
        Assert.NotNull(project);
        Assert.Equal("RED", project.HealthStatus);
    }

    [Fact]
    public async Task EvaluateAllProjectsHealthAsync_OneProjectFails_OthersStillUpdated()
    {
        var failingRepo = new FailingProjectRepository(_context, _overdueProjectId);
        var service = CreateProjectService(failingRepo);

        var result = await service.EvaluateAllProjectsHealthAsync();

        Assert.Equal(2, result.EvaluatedCount);
        Assert.Equal(1, result.FailedCount);

        var clean = await _context.Projects.FindAsync(_cleanProjectId);
        Assert.NotNull(clean);
        Assert.Equal("GREEN", clean.HealthStatus);
    }

    public void Dispose() => _context.Dispose();

    private sealed class FailingProjectRepository(PrmDbContext context, long failProjectId) : IProjectRepository
    {
        private readonly ProjectRepository _inner = new(context);

        public Task<Project?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            _inner.GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<Project>> GetAllAsync(CancellationToken cancellationToken = default) =>
            _inner.GetAllAsync(cancellationToken);

        public Task<IReadOnlyList<Project>> GetByManagerUserIdAsync(long managerUserId, CancellationToken cancellationToken = default) =>
            _inner.GetByManagerUserIdAsync(managerUserId, cancellationToken);

        public Task<IReadOnlyList<Project>> GetActiveAsync(CancellationToken cancellationToken = default) =>
            _inner.GetActiveAsync(cancellationToken);

        public Task UpdateHealthStatusAsync(long projectId, string healthStatus, CancellationToken cancellationToken = default)
        {
            if (projectId == failProjectId)
                throw new InvalidOperationException("Simulated health update failure.");

            return _inner.UpdateHealthStatusAsync(projectId, healthStatus, cancellationToken);
        }

        public Task<bool> ExistsByCodeAsync(string projectCode, CancellationToken cancellationToken = default) =>
            _inner.ExistsByCodeAsync(projectCode, cancellationToken);

        public Task AddAsync(Project project, CancellationToken cancellationToken = default) =>
            _inner.AddAsync(project, cancellationToken);

        public Task UpdateAsync(Project project, CancellationToken cancellationToken = default) =>
            _inner.UpdateAsync(project, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _inner.SaveChangesAsync(cancellationToken);
    }
}
