using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class SystemConfigRepository(PrmDbContext context) : ISystemConfigRepository
{
    public async Task<IReadOnlyList<SystemConfiguration>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.SystemConfigurations.OrderBy(c => c.Id).ToListAsync(cancellationToken);

    public Task<SystemConfiguration?> GetByKeyAsync(string configKey, CancellationToken cancellationToken = default) =>
        context.SystemConfigurations.FirstOrDefaultAsync(c => c.ConfigKey == configKey, cancellationToken);

    public Task UpdateAsync(SystemConfiguration configuration, CancellationToken cancellationToken = default)
    {
        context.SystemConfigurations.Update(configuration);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
