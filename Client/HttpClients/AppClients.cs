namespace Client.HttpClients;

public class AppClients
{
    public AppClients(string baseUrl)
    {
        Rest = new RestClient(baseUrl);
        Auth = new AuthClient(Rest);
        Admin = new AdminClient(Rest);
        Manager = new ManagerClient(Rest);
        Employee = new EmployeeClient(Rest);
        Ai = new AiClient(Rest);
    }

    public RestClient Rest { get; }
    public IAuthClient Auth { get; }
    public IAdminClient Admin { get; }
    public IManagerClient Manager { get; }
    public IEmployeeClient Employee { get; }
    public IAiClient Ai { get; }

    public void SetToken(string? token) => Rest.SetToken(token);
}
