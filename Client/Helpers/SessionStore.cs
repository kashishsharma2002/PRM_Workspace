namespace Client.Helpers;

public static class SessionStore
{
    public static string? Token { get; set; }
    public static string? Role { get; set; }
    public static string? FullName { get; set; }
    public static long? UserId { get; set; }
    public static long? EmployeeId { get; set; }
    public static long? ManagerId { get; set; }
    public static bool ForcePasswordChange { get; set; }

    public static void ApplyLogin(string token, string role, string fullName, long userId, long? employeeId, long? managerId, bool forcePasswordChange)
    {
        Token = token;
        Role = role;
        FullName = fullName;
        UserId = userId;
        EmployeeId = employeeId;
        ManagerId = managerId;
        ForcePasswordChange = forcePasswordChange;
    }

    public static void Clear()
    {
        Token = null;
        Role = null;
        FullName = null;
        UserId = null;
        EmployeeId = null;
        ManagerId = null;
        ForcePasswordChange = false;
    }
}
