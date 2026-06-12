using Client.Common;

namespace Client.HttpClients;

public class AuthClient(RestClient restClient) : IAuthClient
{
    public Task<LoginResponse?> LoginAsync(LoginRequest request) =>
        restClient.PostAsync<LoginResponse>(ApiRoutes.AuthLogin, request);

    public Task<LoginResponse?> ChangePasswordAsync(ChangePasswordRequest request) =>
        restClient.PostAsync<LoginResponse>(ApiRoutes.AuthChangePassword, request, requireAuth: true);
}
