using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Server.Common;
using Server.Common.Allocations;
using Server.Data;
using Server.Models.Entities;
using Server.Services.Employees;
using Tests.Helpers;

namespace Tests;

public class ResourceStatusServiceTests : IDisposable
{
    private readonly PrmDbContext _context;
    private readonly ResourceStatusService _resourceStatusService;
    private readonly long _profileId;

    public ResourceStatusServiceTests()
    {
        var options = new DbContextOptionsBuilder<PrmDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new PrmDbContext(options);
        _profileId = SeedProfile();
        _resourceStatusService = TestServiceFactory.CreateResourceStatusService(_context);
    }

    private long SeedProfile()
    {
        var now = DateTime.UtcNow;
        var user = new User
        {
            Username = "bench.user",
            Email = "bench@example.com",
            FullName = "Bench User",
            PasswordHash = "hash",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var profile = new ResourceProfile
        {
            UserId = user.Id,
            ResourceStatus = ResourceStatusConstants.Allocated,
            CreatedAt = now,
            UpdatedAt = now
        };
        _context.ResourceProfiles.Add(profile);
        _context.SaveChanges();
        return profile.Id;
    }

    [Fact]
    public void ResolveStatusFromUtilization_MapsBenchPartialAndAllocated()
    {
        Assert.Equal(ResourceStatusConstants.Bench, _resourceStatusService.ResolveStatusFromUtilization(0m));
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, _resourceStatusService.ResolveStatusFromUtilization(50m));
        Assert.Equal(ResourceStatusConstants.Allocated, _resourceStatusService.ResolveStatusFromUtilization(100m));
    }

    [Fact]
    public async Task ReconcileAllResourceStatusesAsync_UpdatesBenchWhenNoAllocations()
    {
        var updated = await _resourceStatusService.ReconcileAllResourceStatusesAsync();

        Assert.Equal(1, updated);
        var profile = await _context.ResourceProfiles.FindAsync(_profileId);
        Assert.Equal(ResourceStatusConstants.Bench, profile!.ResourceStatus);
    }

    [Fact]
    public async Task ApplyStatusFromActiveAllocationsAsync_SetsPartiallyAllocated()
    {
        var now = DateTime.UtcNow;
        _context.ProjectAllocations.Add(new ProjectAllocation
        {
            ResourceProfileId = _profileId,
            ProjectId = 1,
            AllocationPercentage = 50,
            AllocationStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            AllocationEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
            AllocationStatus = AllocationStatusConstants.Active,
            AllocatedByUserId = 1,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync();

        await _resourceStatusService.ApplyStatusFromActiveAllocationsAsync(_profileId);

        var profile = await _context.ResourceProfiles.FindAsync(_profileId);
        Assert.Equal(ResourceStatusConstants.PartiallyAllocated, profile!.ResourceStatus);
    }

    public void Dispose() => _context.Dispose();
}
