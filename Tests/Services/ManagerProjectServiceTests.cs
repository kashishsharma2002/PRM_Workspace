using Microsoft.EntityFrameworkCore;
using Tests.Helpers;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Common;
using Server.Data;
using Server.Exceptions;
using Server.Models.Entities;

namespace Tests;

public class ManagerProjectServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ProjectService _projectService;
    private readonly long _ankitUserId;
    private readonly long _nehaUserId;
    private readonly long _ankitProjectId;
    private readonly long _nehaProjectId;

    public ManagerProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        (_ankitUserId, _nehaUserId, _ankitProjectId, _nehaProjectId) = SeedData();

        _projectService = new ProjectService(
            new ProjectRepository(_context),
            new MilestoneRepository(_context),
            new UserRepository(_context),
            new AllocationRepository(_context),
            new EmployeeRepository(_context),
            new TimesheetRepository(_context),
            new SystemConfigRepository(_context),
            TestServiceFactory.CreateAuditService(_context),
            TestServiceFactory.CreateLogger<ProjectService>());
    }

    private (long ankitId, long nehaId, long ankitProjectId, long nehaProjectId) SeedData()
    {
        var now = DateTime.UtcNow;

        var ankit = new User
        {
            Username = "ankit.shah",
            Email = "ankit@techserve.com",
            FullName = "Ankit Shah",
            PasswordHash = "hash",
            Role = "MANAGER",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var neha = new User
        {
            Username = "neha.joshi",
            Email = "neha@techserve.com",
            FullName = "Neha Joshi",
            PasswordHash = "hash",
            Role = "MANAGER",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.AddRange(ankit, neha);
        _context.SaveChanges();

        var alpha = new Project
        {
            ProjectCode = "PRJ-000001",
            ProjectName = "Alpha Portal",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = ankit.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var beta = new Project
        {
            ProjectCode = "PRJ-000002",
            ProjectName = "Beta CRM",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 8, 15),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = ankit.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var gamma = new Project
        {
            ProjectCode = "PRJ-000003",
            ProjectName = "Gamma Rewrite",
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 7, 1),
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            ManagerUserId = neha.Id,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Projects.AddRange(alpha, beta, gamma);
        _context.SaveChanges();

        _context.ProjectMilestones.Add(new ProjectMilestone
        {
            ProjectId = alpha.Id,
            MilestoneTitle = "Backend API",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            MilestoneStatus = "IN_PROGRESS",
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        });
        _context.SaveChanges();

        return (ankit.Id, neha.Id, alpha.Id, gamma.Id);
    }

    [Fact]
    public async Task GetMyProjectsAsync_ReturnsOnlyOwnedProjects()
    {
        var result = await _projectService.GetMyProjectsAsync(_ankitUserId);

        Assert.Equal(2, result.Projects.Count);
        Assert.All(result.Projects, p => Assert.Contains(p.ProjectName, new[] { "Alpha Portal", "Beta CRM" }));
        Assert.DoesNotContain(result.Projects, p => p.ProjectName == "Gamma Rewrite");
    }

    [Fact]
    public async Task GetManagerProjectDetailAsync_OtherManagerProject_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _projectService.GetManagerProjectDetailAsync(_nehaUserId, _ankitProjectId));
    }

    [Fact]
    public async Task GetManagerProjectDetailAsync_OverdueMilestone_SetsRiskFlag()
    {
        var detail = await _projectService.GetManagerProjectDetailAsync(_ankitUserId, _ankitProjectId);

        Assert.Contains("OVERDUE_MILESTONE", detail.RiskFlags);
        Assert.Contains(detail.Milestones, m => m.IsOverdue);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
