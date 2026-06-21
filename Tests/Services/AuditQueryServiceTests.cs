using Moq;
using Server.Common.Audit;
using Server.Common.Roles;
using Server.Models.Entities;
using Server.Models.Queries;
using Server.Repositories.Roles;
using Server.Repositories.Shared;
using Server.Repositories.Users;
using Server.Services.Audit;
using Xunit;

namespace Tests.Services;

public class AuditQueryServiceTests
{
    [Fact]
    public async Task GetAuditLogsAsync_UsesStoredSummaryWhenPresent()
    {
        var auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleRepositoryMock = new Mock<IRoleRepository>();

        var createdAt = new DateTime(2026, 6, 17, 10, 30, 0, DateTimeKind.Utc);
        var auditLog = new AuditLog
        {
            Id = 1,
            ActorUserId = 5,
            EntityName = AuditEntityConstants.Users,
            EntityId = 10,
            ActionType = AuditActionConstants.Update,
            Summary = "Changed Amit Patel's role from Employee to Manager",
            CreatedAt = createdAt
        };

        auditLogRepositoryMock.Setup(r => r.QueryAsync(It.IsAny<AuditLogQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<AuditLog> { auditLog }, 1));

        userRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, User>
            {
                [5] = new User { Id = 5, FullName = "Priya Sharma" }
            });

        roleRepositoryMock.Setup(r => r.GetRoleNamesForUsersAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<long, string> { [5] = RoleConstants.Admin });

        var service = new AuditQueryService(
            auditLogRepositoryMock.Object,
            userRepositoryMock.Object,
            roleRepositoryMock.Object);

        var result = await service.GetAuditLogsAsync(new AuditLogQuery());

        Assert.Single(result.Items);
        Assert.Equal("Changed Amit Patel's role from Employee to Manager", result.Items[0].Summary);
        Assert.Equal("Priya Sharma", result.Items[0].ActorName);
        Assert.Equal(RoleConstants.Admin, result.Items[0].ActorRole);
    }
}
