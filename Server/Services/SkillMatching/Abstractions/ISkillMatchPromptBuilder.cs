using Server.Models.DTOs.SkillMatching;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchPromptBuilder
{
    string BuildSkillMatchPrompt(long projectId, string? requirement, string jsonContext);
    string BuildAtRiskSkillMatchPrompt(long projectId, string? requirement, string jsonContext);
    string BuildOrganizationalSkillMatchPrompt(string? requirement, string jsonContext);
}
