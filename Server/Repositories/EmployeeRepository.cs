using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Entities;
using Server.Repositories.Interfaces;

namespace Server.Repositories;

public class EmployeeRepository(PrmDbContext context) : IEmployeeRepository
{
    public Task<Employee?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        context.Employees.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.Employees.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await context.Employees.AddAsync(employee, cancellationToken);
    }

    public Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        context.Employees.Update(employee);
        return Task.CompletedTask;
    }
}
