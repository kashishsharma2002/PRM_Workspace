using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Server.Data;
using Server.Repositories.Shared;
using Server.Services.Shared;

namespace Tests.Helpers;

public static class TestServiceFactory
{
    public static IAuditService CreateAuditService(PrmDbContext context) =>
        new AuditService(new AuditLogRepository(context));

    public static ILogger<T> CreateLogger<T>() => NullLogger<T>.Instance;
}
