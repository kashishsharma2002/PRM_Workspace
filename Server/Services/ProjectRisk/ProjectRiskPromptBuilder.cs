using Server.Services.ProjectRisk.Abstractions;

namespace Server.Services.ProjectRisk;

public class ProjectRiskPromptBuilder : IProjectRiskPromptBuilder
{
    public string BuildRiskSummaryPrompt(long projectId, string jsonContext) => $@"
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
}
