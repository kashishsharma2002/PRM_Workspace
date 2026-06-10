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

    public async Task<IReadOnlyList<Employee>> GetAllAsync(
        string? employmentStatus,
        string? department,
        CancellationToken cancellationToken = default)
    {
        var query = context.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(employmentStatus))
            query = query.Where(e => e.EmploymentStatus == employmentStatus.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(department))
            query = query.Where(e => e.Department != null && e.Department == department.Trim());

        return await query.OrderBy(e => e.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetByManagerIdAsync(
        long managerUserId,
        CancellationToken cancellationToken = default) =>
        await context.Employees
            .Where(e => e.ManagerId == managerUserId && e.IsActive)
            .OrderBy(e => e.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await context.Employees.AddAsync(employee, cancellationToken);
    }

    public Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        context.Employees.Update(employee);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
