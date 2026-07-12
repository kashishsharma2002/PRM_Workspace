using Server.Common;
using Server.Services.SkillMatching.Abstractions;

namespace Server.Services.SkillMatching;

public class SkillMatchPromptBuilder : ISkillMatchPromptBuilder
{
    private const string DefaultSkillMatchRequirement =
        "Find the best matching resources based on skills required for this project.";

    public string BuildSkillMatchPrompt(long projectId, string? requirement, string jsonContext) => $@"
You are an AI Resource Matching Assistant for the PRM Platform.
You need to match employees from the organization against the project requirements.

Manager's Natural Language Requirement:
""{requirement ?? DefaultSkillMatchRequirement}""

Database State Context (JSON Format):
{jsonContext}

Tasks:
1. Review the list of candidate employees, their skills (including category), and remaining capacity (remainingCapacityPercentage).
2. Only return employees from the candidates list whose skills genuinely match the requirement or project description.
3. Use the exact skill name from the candidate profile in skillName (do not invent skills).
4. Prefer candidates with higher remaining capacity when skills are comparable.
5. For each match, provide the employee's name, the primary matching skill name, a match score between 0 and 100, and a brief reason.

Output Format:
You MUST respond strictly with a valid JSON block containing the properties below and no other conversational wrapper or markdown:
{{
  ""projectId"": {projectId},
  ""matches"": [
    {{
      ""employeeName"": ""Employee Full Name"",
      ""skillName"": ""Matching Skill Name"",
      ""matchScore"": 90,
      ""reason"": ""Brief explanation for the match""
    }}
  ]
}}
";

    public string BuildAtRiskSkillMatchPrompt(long projectId, string? requirement, string jsonContext) => $@"
You are an AI Resource Matching Assistant for the PRM Platform.
A project has turned RED (at risk). Suggest additional resources who are NOT already on this project.

Manager's Natural Language Requirement:
""{requirement ?? "Recommend unallocated available resources to mitigate project risks."}""

Database State Context (JSON Format):
{jsonContext}

Important:
- Every candidate in the context is already NOT allocated to this project and has remaining capacity.
- Prefer fully available candidates (remainingCapacityPercentage = {AllocationConstants.MaxUtilizationPercentage}) when skills are comparable.
- Do not suggest anyone not listed in the candidates array.

Tasks:
1. Review candidate skills and remainingCapacityPercentage.
2. Rank the best matches for mitigating project risk.
3. For each match, provide employee name, primary matching skill, match score 0-100, and a brief reason.

Output Format:
You MUST respond strictly with a valid JSON block containing the properties below and no other conversational wrapper or markdown:
{{
  ""projectId"": {projectId},
  ""matches"": [
    {{
      ""employeeName"": ""Employee Full Name"",
      ""skillName"": ""Matching Skill Name"",
      ""matchScore"": 90,
      ""reason"": ""Brief explanation for the match""
    }}
  ]
}}
";

    public string BuildOrganizationalSkillMatchPrompt(string? requirement, string jsonContext) => $@"
You are an AI Resource Matching Assistant for the PRM Platform.
You need to match employees across the entire organization against the manager's requirement.

Manager's Natural Language Requirement:
""{requirement ?? DefaultSkillMatchRequirement}""

Database State Context (JSON Format):
{jsonContext}

Tasks:
1. Review the list of candidate employees, their skills (including category), and remaining capacity (remainingCapacityPercentage).
2. Only return employees from the candidates list whose skills genuinely match the requirement.
3. Use the exact skill name from the candidate profile in skillName (do not invent skills).
4. Prefer candidates with higher remaining capacity when skills are comparable.
5. For each match, provide the employee's name, the primary matching skill name, a match score between 0 and 100, and a brief reason.

Output Format:
You MUST respond strictly with a valid JSON block containing the properties below and no other conversational wrapper or markdown:
{{
  ""projectId"": 0,
  ""matches"": [
    {{
      ""employeeName"": ""Employee Full Name"",
      ""skillName"": ""Matching Skill Name"",
      ""matchScore"": 90,
      ""reason"": ""Brief explanation for the match""
    }}
  ]
}}
";
}
