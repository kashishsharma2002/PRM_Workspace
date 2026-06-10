using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;

namespace Server.Repositories.Timesheets;

public class ActivityTagRepository(PrmDbContext context) : IActivityTagRepository
{
    public async Task<IReadOnlyList<ActivityTag>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await context.ActivityTags
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public Task<ActivityTag?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.ActivityTags.FirstOrDefaultAsync(t => t.Id == id && t.IsActive, cancellationToken);

    public async Task<IReadOnlyList<ActivityTag>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return [];

        return await context.ActivityTags
            .Where(t => t.IsActive && idList.Contains(t.Id))
            .ToListAsync(cancellationToken);
    }
}
