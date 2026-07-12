namespace Server.Common;

public static class ConfigKeys
{
    public const string LlmProvider = "llm_provider";
    public const string LlmApiKey = "llm_api_key";
    public const string LlmApiKeyGemini = "llm_api_key_gemini";
    public const string LlmApiKeyGroq = "llm_api_key_groq";
    public const string LlmEndpointGemma = "llm_endpoint_gemma";
    public const string LlmModelGemini = "llm_model_gemini";
    public const string LlmModelGroq = "llm_model_groq";
    public const string LlmModelGemma = "llm_model_gemma";
    public const string SchedulerIntervalHours = "scheduler_interval_hours";
    public const string MaxWeeklyHours = "max_weekly_hours";

    public const string TimesheetDeadlineDay = "timesheet_compliance_deadline_day";
}
