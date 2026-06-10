namespace Client.HttpClients;

public class AuthClient(RestClient restClient) : IAuthClient
{
    public Task<LoginResponse?> LoginAsync(LoginRequest request) =>
        restClient.PostAsync<LoginResponse>("/api/auth/login", request);

    public async Task ChangePasswordAsync(ChangePasswordRequest request) =>
        await restClient.PostAsync<object>("/api/auth/change-password", request, requireAuth: true);
}
