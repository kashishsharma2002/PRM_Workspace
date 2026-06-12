using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.AI;
using Server.Common;
using Server.Common.Roles;
using Server.Data;
using Server.Exceptions;
using Server.Models.Entities;
using Server.Repositories.Ai;
using Server.Repositories.Projects;
using Server.Repositories.SystemConfig;
using Server.Services.Ai;
using Tests.Helpers;

namespace Tests;

public class AiIntegrationServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly AiIntegrationService _service;
    private readonly StubLlmClient _llmClient = new();

    public AiIntegrationServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        TestDataHelper.SeedRolesAsync(_context).GetAwaiter().GetResult();

        var aiContextRepository = new AiContextRepository(_context);
        var contextBuilder = new AiContextBuilder(aiContextRepository);
        var responseParser = new AiResponseParser(TestServiceFactory.CreateLogger<AiResponseParser>());
        var teamBuilderResponseNormalizer = new TeamBuilderResponseNormalizer();
        var factory = new LlmClientFactory([_llmClient]);

        _service = new AiIntegrationService(
            new ProjectRepository(_context),
            factory,
            new SystemConfigRepository(_context),
            new AiRequestLogRepository(_context),
            contextBuilder,
            responseParser,
            teamBuilderResponseNormalizer,
            TestServiceFactory.CreateLogger<AiIntegrationService>());
    }

    [Fact]
    public async Task GetRiskSummaryAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        var managerId = await SeedManagerAsync();
        var otherManagerId = await SeedManagerAsync("other.mgr", "other@techserve.com");
        var projectId = await SeedProjectAsync(otherManagerId);

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetRiskSummaryAsync(managerId, projectId));
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsValidation_WhenRequirementTooLong()
    {
        var managerId = await SeedManagerAsync();
        var projectId = await SeedProjectAsync(managerId);
        var longRequirement = new string('x', 501);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            _service.GetSkillMatchAsync(managerId, projectId, longRequirement));
    }

    [Fact]
    public async Task GetSkillMatchAsync_ThrowsNotFound_WhenManagerDoesNotOwnProject()
    {
        var managerId = await SeedManagerAsync();
        var otherManagerId = await SeedManagerAsync("other.skill.mgr", "other.skill@techserve.com");
        var projectId = await SeedProjectAsync(otherManagerId);

        await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _service.GetSkillMatchAsync(managerId, projectId, "Need a developer"));
    }

    [Fact]
    public async Task GetOrganizationalSkillMatchAsync_ReturnsMatches_WithoutProjectOwnership()
    {
        var managerId = await SeedManagerAsync();
        var otherManagerId = await SeedManagerAsync("other.org.mgr", "other.org@techserve.com");
        await SeedProjectAsync(otherManagerId);

        _llmClient.NextResponse = """
            {
              "projectId": 0,
              "matches": [
                {
                  "employeeName": "Org Employee",
                  "skillName": "React",
                  "matchScore": 91,
                  "reason": "Available React developer"
                }
              ]
            }
            """;

        var result = await _service.GetOrganizationalSkillMatchAsync(managerId, "Need a React developer");

        Assert.Equal(0, result.ProjectId);
        Assert.Single(result.Matches);
        Assert.Equal("Org Employee", result.Matches[0].EmployeeName);
        Assert.True(await _context.AiRequestLogs.AnyAsync());
    }

    [Fact]
    public async Task GetSkillMatchAsync_ReturnsMatches_AndLogsRequest()
    {
        var managerId = await SeedManagerAsync();
        var projectId = await SeedProjectAsync(managerId);

        _llmClient.NextResponse = """
            {
              "projectId": 1,
              "matches": [
                {
                  "employeeName": "Test Employee",
                  "skillName": "C#",
                  "matchScore": 88,
                  "reason": "Strong match"
                }
              ]
            }
            """;

        var result = await _service.GetSkillMatchAsync(managerId, projectId, "Need a C# developer");

        Assert.Single(result.Matches);
        Assert.Equal("Test Employee", result.Matches[0].EmployeeName);
        Assert.True(await _context.AiRequestLogs.AnyAsync());
    }

    [Fact]
    public async Task BuildTeamAsync_ReturnsRoles_AndLogsRequest()
    {
        var managerId = await SeedManagerAsync();

        _llmClient.NextResponse = """
            {
              "roles": [
                {
                  "roleTitle": "QA Tester",
                  "requiredSkills": [{"skillName": "Selenium", "minProficiency": "BEGINNER"}],
                  "status": "GAP",
                  "gap": {
                    "reasonType": "NO_SKILL",
                    "message": "No employee has Selenium skills."
                  }
                }
              ]
            }
            """;

        var result = await _service.BuildTeamAsync(
            managerId,
            "For a banking portal we need a QA Tester with Selenium.");

        Assert.Single(result.Roles);
        Assert.Equal("QA Tester", result.Roles[0].RoleTitle);
        Assert.Equal("GAP", result.Roles[0].Status);
        Assert.True(await _context.AiRequestLogs.AnyAsync(l => l.RequestType == "TEAM_BUILDER"));
    }

    [Fact]
    public async Task BuildTeamAsync_RetriesOnce_WhenInitialParseFails()
    {
        var managerId = await SeedManagerAsync();

        _llmClient.EnqueueResponse("not valid json");
        _llmClient.EnqueueResponse("""
            {
              "roles": [
                {
                  "roleTitle": "QA Tester",
                  "requiredSkills": [{"skillName": "Selenium", "minProficiency": "BEGINNER"}],
                  "status": "GAP",
                  "gap": {
                    "reasonType": "NO_SKILL",
                    "message": "No employee has Selenium skills."
                  }
                }
              ]
            }
            """);

        var result = await _service.BuildTeamAsync(
            managerId,
            "For a banking portal we need a QA Tester with Selenium.");

        Assert.Single(result.Roles);
        Assert.Equal("GAP", result.Roles[0].Status);
        Assert.Equal(2, _llmClient.CallCount);
    }

    [Fact]
    public async Task BuildTeamAsync_NormalizesDuplicateAssignment_ToGap()
    {
        var managerId = await SeedManagerAsync();
        await SeedBenchEmployeeAsync("Aarav Patel");

        _llmClient.NextResponse = """
            {
              "roles": [
                {
                  "roleTitle": "Senior React Developer (1)",
                  "requiredSkills": [{"skillName": "React", "minProficiency": "ADVANCED"}],
                  "status": "FILLED",
                  "assignedEmployeeName": "Aarav Patel",
                  "matchScore": 90,
                  "reason": "React expert on bench."
                },
                {
                  "roleTitle": "Senior React Developer (2)",
                  "requiredSkills": [{"skillName": "React", "minProficiency": "ADVANCED"}],
                  "status": "FILLED",
                  "assignedEmployeeName": "Aarav Patel",
                  "matchScore": 85,
                  "reason": "Only React developer available."
                }
              ]
            }
            """;

        var result = await _service.BuildTeamAsync(
            managerId,
            "Banking portal with 2 Senior React Developers.");

        Assert.Equal(2, result.Roles.Count);
        Assert.Equal("FILLED", result.Roles[0].Status);
        Assert.Equal("GAP", result.Roles[1].Status);
        Assert.Equal("ALREADY_ASSIGNED_IN_TEAM", result.Roles[1].Gap?.ReasonType);
    }

    private async Task SeedBenchEmployeeAsync(string fullName)
    {
        var employeeRole = await _context.Roles.FirstAsync(r => r.RoleName == RoleConstants.Employee);
        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = $"{fullName.Replace(" ", ".").ToLowerInvariant()}@techserve.com",
            Username = fullName.Replace(" ", ".").ToLowerInvariant(),
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.UserRoles.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = employeeRole.Id,
            AssignedAt = now
        });
        await _context.ResourceProfiles.AddAsync(new ResourceProfile
        {
            UserId = user.Id,
            ResourceStatus = "BENCH",
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();
    }

    private async Task<long> SeedManagerAsync(string username = "ai.mgr", string email = "ai.mgr@techserve.com")
    {
        var managerRole = await _context.Roles.FirstAsync(r => r.RoleName == RoleConstants.Manager);
        var now = DateTime.UtcNow;
        var user = new User
        {
            FullName = "AI Manager",
            Email = email,
            Username = username,
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await _context.UserRoles.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = managerRole.Id,
            AssignedAt = now
        });
        await _context.SaveChangesAsync();
        return user.Id;
    }

    private async Task<long> SeedProjectAsync(long managerUserId)
    {
        var project = new Project
        {
            ProjectName = "AI Test Project",
            ProjectCode = $"PRJ-{Guid.NewGuid():N}"[..12],
            ManagerUserId = managerUserId,
            ProjectStatus = "ACTIVE",
            HealthStatus = "GREEN",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        return project.Id;
    }

    public void Dispose() => _context.Dispose();

    private sealed class StubLlmClient : ILlmClient
    {
        private readonly Queue<string> _queuedResponses = new();

        public string ProviderKey => LlmProviders.Gemini;
        public string NextResponse { get; set; } = "{}";
        public int CallCount { get; private set; }

        public void EnqueueResponse(string response) => _queuedResponses.Enqueue(response);

        public Task<string> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (_queuedResponses.Count > 0)
                return Task.FromResult(_queuedResponses.Dequeue());

            return Task.FromResult(NextResponse);
        }
    }
}
