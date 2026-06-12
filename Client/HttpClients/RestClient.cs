using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Client.Helpers;
using Client.Models;

namespace Client.HttpClients;

public class RestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var response = await _http.PostAsJsonAsync($"{_baseUrl}{endpoint}", payload, JsonOptions);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object payload, bool requireAuth = false)
    {
        if (requireAuth && string.IsNullOrWhiteSpace(SessionStore.Token))
            throw new SessionExpiredException("Session expired. Please log in again.");

        var response = await _http.PutAsJsonAsync($"{_baseUrl}{endpoint}", payload, JsonOptions);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> DeleteAsync<T>(string endpoint, bool requireAuth = false)
    {
        if (requireAuth && string.IsNullOrWhiteSpace(SessionStore.Token))
            throw new SessionExpiredException("Session expired. Please log in again.");

        var response = await _http.DeleteAsync($"{_baseUrl}{endpoint}");
        return await HandleResponse<T>(response);
    }

    private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        try
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var unauthorizedEnvelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
                var message = unauthorizedEnvelope?.Error ?? "Invalid username or password.";

                if (!string.IsNullOrWhiteSpace(SessionStore.Token))
                {
                    SessionStore.Clear();
                    throw new SessionExpiredException("Session expired. Please log in again.");
                }

                throw new ApiClientException(message, unauthorizedEnvelope?.ErrorCode, (int)response.StatusCode);
            }

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
            if (envelope is null)
                throw new ApiClientException("Empty response from server.", null, (int)response.StatusCode);

            if (!response.IsSuccessStatusCode || !envelope.Success)
            {
                var details = envelope.Details is not null ? string.Join("; ", envelope.Details) : envelope.Error;
                throw new ApiClientException(details ?? "Request failed.", envelope.ErrorCode, (int)response.StatusCode);
            }

            return envelope.Data;
        }
        catch (JsonException ex)
        {
            var contentPreview = await response.Content.ReadAsStringAsync();
            var preview = contentPreview.Length > 100 ? $"{contentPreview[..100]}..." : contentPreview;
            throw new ApiClientException(
                $"Server returned an unexpected response (HTTP {(int)response.StatusCode}). " +
                $"This may indicate an API route mismatch or server error. Response: {preview}",
                "INVALID_JSON_RESPONSE",
                (int)response.StatusCode,
                ex);
        }
    }
}
