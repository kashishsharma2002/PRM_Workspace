using Server.Models.DTOs.Allocations;
using Server.Validators.Allocations;

namespace Tests;

public class CreateAllocationRequestValidatorTests
{
    private readonly CreateAllocationRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = 50,
            AllocationStartDate = today.AddDays(1),
            AllocationEndDate = today.AddDays(30)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EndBeforeStart_Fails()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = 50,
            AllocationStartDate = today.AddDays(30),
            AllocationEndDate = today.AddDays(1)
        });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_InvalidPercentage_Fails(decimal percentage)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = percentage,
            AllocationStartDate = today.AddDays(1),
            AllocationEndDate = today.AddDays(30)
        });

        Assert.False(result.IsValid);
    }
}
