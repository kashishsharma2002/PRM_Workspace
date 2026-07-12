using Server.Models.DTOs.SkillMatching;

namespace Server.Services.SkillMatching.Abstractions;

public interface ISkillMatchResponseParser
{
    AiSkillMatchResponseDto ParseSkillMatch(string responseText, long projectId);
}
