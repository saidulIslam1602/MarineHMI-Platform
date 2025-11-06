using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KChief.Platform.Core.Utilities;

/// <summary>
/// Helper class for HTTP client operations.
/// </summary>
public static class HttpClientHelper
{
    /// <summary>
    /// Creates an HTTP client with default configuration.
    /// </summary>
    public static HttpClient CreateClient(string? baseAddress = null, TimeSpan? timeout = null)
    {
        var client = new HttpClient();
        
        if (!string.IsNullOrEmpty(baseAddress))
        {
            client.BaseAddress = new Uri(baseAddress);
        }

        if (timeout.HasValue)
        {
            client.Timeout = timeout.Value;
        }

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    /// <summary>
    /// Creates an HTTP client with authentication.
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(string token, string? baseAddress = null)
    {
        var client = CreateClient(baseAddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HTTP client with API key authentication.
    /// </summary>
    public static HttpClient CreateApiKeyClient(string apiKey, string? baseAddress = null)
    {
        var client = CreateClient(baseAddress);
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        return client;
    }

    /// <summary>
    /// Sends a GET request and deserializes the response.
    /// </summary>
    public static async Task<T?> GetAsync<T>(HttpClient client, string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await client.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonHelper.Deserialize<T>(content);
    }

    /// <summary>
    /// Sends a POST request with JSON body.
    /// </summary>
    public static async Task<TResponse?> PostAsync<TRequest, TResponse>(
        HttpClient client,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var json = JsonHelper.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonHelper.Deserialize<TResponse>(responseContent);
    }

    /// <summary>
    /// Sends a PUT request with JSON body.
    /// </summary>
    public static async Task<TResponse?> PutAsync<TRequest, TResponse>(
        HttpClient client,
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var json = JsonHelper.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonHelper.Deserialize<TResponse>(responseContent);
    }

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    public static async Task DeleteAsync(HttpClient client, string endpoint, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

