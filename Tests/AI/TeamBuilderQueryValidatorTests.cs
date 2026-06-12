using Server.Common.Ai;
using Server.Validators.Ai;

namespace Tests;

public class TeamBuilderQueryValidatorTests
{
    private readonly TeamBuilderQueryValidator _validator = new();

    [Fact]
    public async Task Validate_Fails_WhenRequirementEmpty()
    {
        var result = await _validator.ValidateAsync(new TeamBuilderQuery { Requirement = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(TeamBuilderQuery.Requirement));
    }

    [Fact]
    public async Task Validate_Fails_WhenRequirementWhitespaceOnly()
    {
        var result = await _validator.ValidateAsync(new TeamBuilderQuery { Requirement = "   " });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_Fails_WhenRequirementTooLong()
    {
        var result = await _validator.ValidateAsync(new TeamBuilderQuery
        {
            Requirement = new string('x', AiValidationLimits.MaxTeamBuilderRequirementLength + 1)
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_Passes_WhenRequirementValid()
    {
        var result = await _validator.ValidateAsync(new TeamBuilderQuery
        {
            Requirement = "Need a Senior Java Developer and a QA Tester for a banking portal."
        });

        Assert.True(result.IsValid);
    }
}
