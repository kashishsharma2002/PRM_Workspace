using System.Threading;
using System.Threading.Tasks;

namespace Server.Common;

public interface IDbTransactionManager
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
