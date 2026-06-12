namespace Tests.Helpers;

public static class MockData
{
    public const string EmailDomain = "mock.test";

    public static class Names
    {
        public const string ManagerA = "Mock Manager A";
        public const string ManagerB = "Mock Manager B";
        public const string EmployeeA = "Mock Employee A";
        public const string EmployeeB = "Mock Employee B";
        public const string EmployeeC = "Mock Employee C";
        public const string BenchEmployee = "Mock Bench Employee";
        public const string AllocatedEmployee = "Mock Allocated Employee";
        public const string OtherManager = "Mock Other Manager";
        public const string AiManager = "Mock AI Manager";

        public const string ProjectA = "Mock Project A";
        public const string ProjectB = "Mock Project B";
        public const string ProjectC = "Mock Project C";
        public const string TestProject = "Mock Test Project";
        public const string CleanProject = "Mock Clean Project";
        public const string OverdueProject = "Mock Overdue Project";
        public const string MultiFlagProject = "Mock Multi Flag Project";

        public const string TeamBuilderEmployee = "Mock Team Builder Employee";
        public const string TeamBuilderAllocatedEmployee = "Mock Allocated Team Employee";
        public const string TeamBuilderJavaEmployee = "Mock Java Employee";
    }

    public static string Email(string key) => $"{Username(key)}@{EmailDomain}";

    public static string Username(string key) => key.Replace(' ', '.').ToLowerInvariant();
}
