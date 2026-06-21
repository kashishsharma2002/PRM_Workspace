using Server.Common.Audit;
using Server.Common.Roles;
using Xunit;

namespace Tests.Audit;

public class AuditMessageBuilderTests
{
    [Fact]
    public void BuildRoleChangeSummary_FormatsFriendlyMessage()
    {
        var summary = AuditMessageBuilder.BuildRoleChangeSummary(
            "Amit Patel",
            RoleConstants.Employee,
            RoleConstants.Manager);

        Assert.Equal("Changed Amit Patel's role from Employee/Resource to Manager", summary);
    }

    [Fact]
    public void BuildLoginSummary_DistinguishesSuccessAndFailure()
    {
        Assert.Contains("signed in successfully", AuditMessageBuilder.BuildLoginSummary("Priya Sharma", true));
        Assert.Contains("Failed sign-in", AuditMessageBuilder.BuildLoginSummary("unknown.user", false));
    }

    [Fact]
    public void BuildFromStoredValues_RoleChangeFallback_WorksWithoutSummaryColumn()
    {
        var summary = AuditMessageBuilder.BuildFromStoredValues(
            AuditEntityConstants.Users,
            AuditActionConstants.Update,
            "{\"role\":\"EMPLOYEE\"}",
            "{\"role\":\"MANAGER\"}");

        Assert.Equal("Changed user role from Employee/Resource to Manager", summary);
    }
}
