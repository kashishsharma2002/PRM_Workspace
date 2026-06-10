using Server.Models.DTOs.Allocations;
using Server.Validators.Allocations;

namespace Tests;

public class CreateAllocationRequestValidatorTests
{
    private readonly CreateAllocationRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = 50,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30)
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EndBeforeStart_Fails()
    {
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = 50,
            AllocationStartDate = new DateOnly(2026, 6, 30),
            AllocationEndDate = new DateOnly(2026, 3, 1)
        });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_InvalidPercentage_Fails(decimal percentage)
    {
        var result = _validator.Validate(new CreateAllocationRequestDto
        {
            EmployeeId = 1,
            ProjectId = 2,
            AllocationPercentage = percentage,
            AllocationStartDate = new DateOnly(2026, 3, 1),
            AllocationEndDate = new DateOnly(2026, 6, 30)
        });

        Assert.False(result.IsValid);
    }
}
