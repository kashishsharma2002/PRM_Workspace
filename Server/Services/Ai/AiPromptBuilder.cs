namespace Server.Services.Ai;

using Server.Common;
using Server.Common.Ai;

public static class AiPromptBuilder
{
    private const string DefaultSkillMatchRequirement =
        "Find the best matching resources based on skills required for this project.";

    public static string BuildRiskSummaryPrompt(long projectId, string jsonContext) => $@"
You are an AI Risk Analysis Assistant for the PRM Platform.
Analyze the following project state context provided in JSON format:

{jsonContext}

Tasks:
1. Provide a concise paragraph summarizing key project risks (e.g. overdue milestones, low logged hours, timeline pressure, resource utilization issues).
2. Suggest 2-3 specific, actionable recommendations for the manager.

Output Format:
You MUST respond strictly with a valid JSON block containing the properties below and no other conversational wrapper or markdown:
{{
  ""projectId"": {projectId},
  ""summary"": ""your analysis summary here"",
  ""recommendations"": [
    ""recommendation 1"",
    ""recommendation 2""
  ]
}}
";

    public static string BuildSkillMatchPrompt(long projectId, string? requirement, string jsonContext) => $@"
You are an AI Resource Matching Assistant for the PRM Platform.
You need to match employees from the organization against the project requirements.

Manager's Natural Language Requirement:
""{requirement ?? DefaultSkillMatchRequirement}""

Database State Context (JSON Format):
{jsonContext}

Tasks:
1. Review the list of candidate employees, their skills, and remaining capacity (remainingCapacityPercentage).
2. Filter and rank the candidates who best match the project description or the manager's requirement.
3. Prefer candidates with higher remaining capacity when skills are comparable.
4. For each match, provide the employee's name, the primary matching skill name, a match score between 0 and 100, and a brief reason.

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

    public static string BuildAtRiskSkillMatchPrompt(long projectId, string? requirement, string jsonContext) => $@"
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

    public static string BuildOrganizationalSkillMatchPrompt(string? requirement, string jsonContext) => $@"
You are an AI Resource Matching Assistant for the PRM Platform.
You need to match employees across the entire organization against the manager's requirement.

Manager's Natural Language Requirement:
""{requirement ?? DefaultSkillMatchRequirement}""

Database State Context (JSON Format):
{jsonContext}

Tasks:
1. Review the list of candidate employees, their skills, and remaining capacity (remainingCapacityPercentage).
2. Filter and rank the candidates who best match the manager's requirement.
3. Prefer candidates with higher remaining capacity when skills are comparable.
4. For each match, provide the employee's name, the primary matching skill name, a match score between 0 and 100, and a brief reason.

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

    public static string BuildTeamBuilderPrompt(string requirement, string jsonContext) => $@"
You are an AI Team Builder Assistant for the PRM Platform.
The manager describes an entire project team in one natural-language requirement.
You must parse every role from the requirement and fill each role in a single pass.

Manager's Natural Language Team Requirement:
""{requirement}""

Database State Context (JSON Format):
{jsonContext}

The context contains:
- managerRequirement: the raw requirement text
- assignableCandidates: employees with remainingCapacityPercentage = {AllocationConstants.MaxUtilizationPercentage} (fully available)
- allCandidates: full organization pool including partially or fully allocated employees

Phase A — Parse requirement:
1. Extract each distinct role from the requirement (e.g. ""Senior Java Developer"", ""DevOps Engineer"", ""QA Tester"").
2. When a count is specified (e.g. ""2 Senior React Developers""), create that many separate role entries with numbered titles (e.g. ""Senior React Developer (1)"", ""Senior React Developer (2)""). Each counted slot is an independent role in the roles array.
3. For each role, infer required skills and minimum proficiency ({string.Join(", ", EmployeeConstants.ProficiencyLevels)}).
4. Map informal terms: beginner → BEGINNER, intermediate → INTERMEDIATE, advanced/senior → ADVANCED. Default to INTERMEDIATE when not stated.
5. If a role has no inferable skills, use the role title as a skill hint.

Phase B — Match (single pass):
1. Fill every role from assignableCandidates only ({AllocationConstants.MaxUtilizationPercentage}% availability).
2. Never assign the same employee to two roles. If an employee is already assigned to a FILLED role, do not reuse them — mark the extra slot as GAP instead.
3. Match skills case-insensitively; proficiency order: BEGINNER < INTERMEDIATE < ADVANCED.
4. Prefer higher proficiency and designation fit when multiple candidates qualify.
5. For unfilled roles, set status to ""{TeamBuilderConstants.StatusGap}"" with exactly one gap reasonType:
   - ""{TeamBuilderConstants.GapReasonNoSkill}"" — no employee has all required skills at minimum proficiency; state which skills are missing org-wide and suggest hire or train.
   - ""{TeamBuilderConstants.GapReasonAllocatedElsewhere}"" — employee(s) have skills but are not fully available; name the closest match, availableFromDate (latest active allocation end date from context), and how many bench candidates were found.
   - ""{TeamBuilderConstants.GapReasonAlreadyAssignedInTeam}"" — the only matching employee was already assigned to another role in this team plan; name that employee and the role they fill.
6. For filled roles, set status to ""{TeamBuilderConstants.StatusFilled}"".
7. Echo requiredSkills on each role result.
8. GAP messages must be specific: how many bench candidates matched, who the best alternative is, and why the role cannot be filled.

Return only the roles from the manager's requirement — do not copy every example role into your response.

Output Format:
You MUST respond strictly with a valid JSON block and no other conversational wrapper or markdown:
{{
  ""roles"": [
    {{
      ""roleTitle"": ""Senior React Developer (1)"",
      ""requiredSkills"": [
        {{ ""skillName"": ""React"", ""minProficiency"": ""ADVANCED"" }}
      ],
      ""status"": ""{TeamBuilderConstants.StatusFilled}"",
      ""assignedEmployeeName"": ""Aarav Patel"",
      ""matchScore"": 88,
      ""reason"": ""Advanced React on bench."",
      ""gap"": null
    }},
    {{
      ""roleTitle"": ""Senior React Developer (2)"",
      ""requiredSkills"": [
        {{ ""skillName"": ""React"", ""minProficiency"": ""ADVANCED"" }}
      ],
      ""status"": ""{TeamBuilderConstants.StatusGap}"",
      ""assignedEmployeeName"": null,
      ""matchScore"": null,
      ""reason"": null,
      ""gap"": {{
        ""reasonType"": ""{TeamBuilderConstants.GapReasonAlreadyAssignedInTeam}"",
        ""message"": ""Aarav Patel is already assigned to Senior React Developer (1). No other fully available employee matches React at the required level. Consider hiring or training."",
        ""alternativeEmployeeName"": ""Aarav Patel"",
        ""availableFromDate"": null
      }}
    }}
  ]
}}
";

    public static string BuildTeamBuilderRepairPrompt(string requirement, string previousResponse) => $@"
Your previous response was invalid or incomplete JSON and could not be parsed.

Manager's Team Requirement:
""{requirement}""

Previous response (may be truncated or malformed):
{previousResponse}

Return ONLY a valid JSON object with a ""roles"" array covering every role from the manager's requirement.
Do not include markdown, code fences, or any text outside the JSON object.
Each role must include: roleTitle, requiredSkills, status ({TeamBuilderConstants.StatusFilled} or {TeamBuilderConstants.StatusGap}),
and for FILLED roles assignedEmployeeName, matchScore, reason; for GAP roles a gap object with reasonType and message.
Never assign the same employee to more than one FILLED role.
";
}
