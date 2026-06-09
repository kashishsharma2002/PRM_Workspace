using Server.Models.Entities;

namespace Server.Repositories.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetAllAsync(string? employmentStatus, string? department, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
