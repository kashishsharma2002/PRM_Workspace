using Server.Common.Permissions;
using Server.Common.Roles;
using Xunit;

namespace Tests.Authorization;

public class RolePermissionParityTests
{
    [Theory]
    [InlineData(RoleConstants.Admin, 30)]
    [InlineData(RoleConstants.Manager, 10)]
    [InlineData(RoleConstants.Employee, 4)]
    public void SeedData_ContainsExpectedPermissionCount(string roleName, int expectedCount)
    {
        var permissions = PermissionSeedData.GetPermissionCodesForRole(roleName);
        Assert.Equal(expectedCount, permissions.Count);
    }

    [Fact]
    public void SeedData_AdminContainsAuditAndRoleReadPermissions()
    {
        var permissions = PermissionSeedData.GetPermissionCodesForRole(RoleConstants.Admin);

        Assert.Contains(PermissionConstants.Code(PermissionConstants.Roles.Resource, PermissionConstants.Roles.Read), permissions);
        Assert.Contains(PermissionConstants.Code(PermissionConstants.AuditLogs.Resource, PermissionConstants.AuditLogs.Read), permissions);
    }

    [Fact]
    public void SeedData_DoesNotContainRemovedPermissions()
    {
        var allCodes = PermissionSeedData.AllPermissions.Select(PermissionSeedData.ToCode).ToList();

        Assert.DoesNotContain("allocations:delete", allCodes);
        Assert.DoesNotContain("roles:manage_permissions", allCodes);
    }

    [Fact]
    public void SeedData_ManagerPermissions_MatchExpectedSet()
    {
        var expected = new[]
        {
            "employees:read",
            "projects:read",
            "projects:update",
            "allocations:read",
            "allocations:create",
            "allocations:update",
            "allocations:end",
            "timesheets:read_team",
            "timesheets:review",
            "ai_insights:read"
        };

        var actual = PermissionSeedData.GetPermissionCodesForRole(RoleConstants.Manager);
        Assert.Equal(expected.OrderBy(code => code), actual.OrderBy(code => code));
    }

    [Fact]
    public void SeedData_EmployeePermissions_MatchExpectedSet()
    {
        var expected = new[]
        {
            "timesheets:read_own",
            "timesheets:submit",
            "activity_tags:read",
            "activity_tags:create"
        };

        var actual = PermissionSeedData.GetPermissionCodesForRole(RoleConstants.Employee);
        Assert.Equal(expected.OrderBy(code => code), actual.OrderBy(code => code));
    }

    [Fact]
    public void SeedData_AllPermissionsHaveUniqueCodes()
    {
        var codes = PermissionSeedData.AllPermissions.Select(PermissionSeedData.ToCode).ToList();
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
