using Microsoft.EntityFrameworkCore.Storage;
using Server.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Data;

public class EfDbTransaction(IDbContextTransaction transaction) : IDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() =>
        transaction.DisposeAsync();
}
