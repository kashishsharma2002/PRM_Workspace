using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Data;
using Server.Repositories.Allocations;
using Server.Repositories.Employees;
using Server.Repositories.Shared;
using Server.Services.Employees;
using Server.Services.Shared;

namespace Tests.Helpers;

public static class TestServiceFactory
{
    public static IAuditService CreateAuditService(PrmDbContext context) =>
        new AuditService(new AuditLogRepository(context));

    public static RoleRepository CreateRoleRepository(PrmDbContext context) =>
        new RoleRepository(context);

    public static ResourceStatusService CreateResourceStatusService(PrmDbContext context) =>
        new ResourceStatusService(
            new EmployeeRepository(context),
            new AllocationRepository(context),
            CreateLogger<ResourceStatusService>());

    public static ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;
}
