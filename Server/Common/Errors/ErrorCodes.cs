namespace Server.Common.Errors;

public static class ErrorCodes
{
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string SessionExpired = "AUTH_SESSION_EXPIRED";

    public const string UserNotFound = "USER_NOT_FOUND";
    public const string InvalidManager = "USER_INVALID_MANAGER";

    public const string EmployeeNotFound = "EMPLOYEE_NOT_FOUND";
    public const string EmployeeInactive = "EMPLOYEE_INACTIVE";
    public const string EmployeeNotOnTeam = "EMPLOYEE_NOT_ON_TEAM";

    public const string ProjectNotFound = "PROJECT_NOT_FOUND";
    public const string MilestoneNotFound = "MILESTONE_NOT_FOUND";

    public const string AllocationNotFound = "ALLOCATION_NOT_FOUND";

    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string NotFound = "NOT_FOUND";
    public const string UnexpectedError = "UNEXPECTED_ERROR";
}
