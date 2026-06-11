namespace Server.Common;

public static class DesignationConstants
{
    public const string SystemAdministrator = "SYSTEM_ADMINISTRATOR";
    public const string DeliveryManager = "DELIVERY_MANAGER";
    public const string SoftwareEngineer = "SOFTWARE_ENGINEER";
    public const string SeniorEngineer = "SENIOR_ENGINEER";
    public const string Jse = "JSE";
    public const string TechnicalLead = "TECHNICAL_LEAD";
    public const string TechnicalArchitect = "TECHNICAL_ARCHITECT";

    public static readonly string[] All =
    [
        SystemAdministrator,
        DeliveryManager,
        SoftwareEngineer,
        SeniorEngineer,
        Jse,
        TechnicalLead,
        TechnicalArchitect
    ];

    public static readonly string[] AdminOptions = [SystemAdministrator];

    public static readonly string[] ManagerOptions =
        [DeliveryManager, TechnicalLead, TechnicalArchitect, SeniorEngineer];

    public static readonly string[] EmployeeOptions =
        [SoftwareEngineer, SeniorEngineer, Jse, TechnicalLead, TechnicalArchitect];

    public static string GetDisplayName(string value) => value switch
    {
        SystemAdministrator => "System Administrator",
        DeliveryManager => "Delivery Manager",
        SoftwareEngineer => "Software Engineer",
        SeniorEngineer => "Senior Engineer",
        Jse => "JSE",
        TechnicalLead => "Technical Lead",
        TechnicalArchitect => "Technical Architect",
        _ => value
    };
}
