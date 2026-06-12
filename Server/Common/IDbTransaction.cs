using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Common;

public interface IDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
