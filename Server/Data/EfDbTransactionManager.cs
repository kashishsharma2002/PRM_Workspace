using Microsoft.EntityFrameworkCore;
using Server.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Data;

public class EfDbTransactionManager(PrmDbContext context) : IDbTransactionManager
{
    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfDbTransaction(transaction);
    }
}
