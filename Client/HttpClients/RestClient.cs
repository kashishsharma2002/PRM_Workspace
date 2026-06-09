using System.Net.Http.Headers;
using System.Net.Http.Json;
using Client.Session;

namespace Client.HttpClients;

public class RestClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public RestClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient();
    }

    public void SetToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization = null;
        else
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint, bool requireAuth = false)
    {
        if (requireAuth && string.IsNullOrWhiteSpace(SessionStore.Token))
            throw new SessionExpiredException("Session expired. Please log in again.");

        var response = await _http.GetAsync($"{_baseUrl}{endpoint}");
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object payload, bool requireAuth = false)
    {
        if (requireAuth && string.IsNullOrWhiteSpace(SessionStore.Token))
            throw new SessionExpiredException("Session expired. Please log in again.");

        var response = await _http.PostAsJsonAsync($"{_baseUrl}{endpoint}", payload);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object payload, bool requireAuth = false)
    {
        if (requireAuth && string.IsNullOrWhiteSpace(SessionStore.Token))
            throw new SessionExpiredException("Session expired. Please log in again.");

        var response = await _http.PutAsJsonAsync($"{_baseUrl}{endpoint}", payload);
        return await HandleResponse<T>(response);
    }

    private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var unauthorizedEnvelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            var message = unauthorizedEnvelope?.Error ?? "Invalid username or password.";

            if (!string.IsNullOrWhiteSpace(SessionStore.Token))
            {
                SessionStore.Clear();
                throw new SessionExpiredException("Session expired. Please log in again.");
            }

            throw new InvalidOperationException(message);
        }

        var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        if (envelope is null)
            throw new InvalidOperationException("Empty response from server.");

        if (!response.IsSuccessStatusCode || !envelope.Success)
        {
            var details = envelope.Details is not null ? string.Join("; ", envelope.Details) : envelope.Error;
            throw new InvalidOperationException(details ?? "Request failed.");
        }

        return envelope.Data;
    }
}
