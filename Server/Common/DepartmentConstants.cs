namespace Server.Common;

public static class DepartmentConstants
{
    public const string HrOps = "HR_OPS";
    public const string Management = "MANAGEMENT";
    public const string SoftwareDevelopment = "SOFTWARE_DEVELOPMENT";
    public const string DevOps = "DEVOPS";
    public const string Qa = "QA";
    public const string Sdet = "SDET";
    public const string BiReporting = "BI_REPORTING";
    public const string Dba = "DBA";

    public static readonly string[] All =
    [
        HrOps,
        Management,
        SoftwareDevelopment,
        DevOps,
        Qa,
        Sdet,
        BiReporting,
        Dba
    ];

    public static readonly string[] AdminOptions = [HrOps];

    public static readonly string[] ManagerOptions = All;

    public static readonly string[] EmployeeOptions =
    [
        SoftwareDevelopment,
        DevOps,
        Qa,
        Sdet,
        BiReporting,
        Dba
    ];

    public static string GetDisplayName(string value) => value switch
    {
        HrOps => "HR/OPS",
        Management => "Management",
        SoftwareDevelopment => "Software Development",
        DevOps => "DevOps",
        Qa => "QA",
        Sdet => "SDET",
        BiReporting => "BI & Reporting",
        Dba => "DBA",
        _ => value
    };
}
