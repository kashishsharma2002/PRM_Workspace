namespace Client.HttpClients;

public interface IAuthClient
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> ChangePasswordAsync(ChangePasswordRequest request);
}
