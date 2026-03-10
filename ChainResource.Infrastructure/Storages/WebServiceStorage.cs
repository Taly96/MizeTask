using System.Text.Json;
using ChainResource.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChainResource.Infrastructure.Storages;

public class WebServiceStorage<T> : IStorage<T>
{
    public bool CanWrite => false;

    private readonly HttpClient _httpClient;
    
    private readonly ILogger<WebServiceStorage<T>> _logger;
    
    private readonly string _appId;
    
    private readonly string _baseUrl;

    public WebServiceStorage(HttpClient httpClient, string appId, string baseUrl, ILogger<WebServiceStorage<T>> logger)
    {
        _httpClient = httpClient;
        _appId = appId;
        _baseUrl = baseUrl;
        _logger = logger;
    }

    public async Task<(bool isValid, T? value)> TryGetValueAsync()
    {
        var url = $"{_baseUrl}latest.json?app_id={_appId}";

        try
        {
            _logger.LogInformation("Fetching latest data from OpenExchange API...");

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("API Request failed. Status: {StatusCode}, Reason: {Reason}", 
                    response.StatusCode, response.ReasonPhrase);
                
                return (false, default);
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            var value = await JsonSerializer.DeserializeAsync<T>(contentStream);

            _logger.LogInformation("API request successful");

            return (value != null, value);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "Network error occurred while calling the API.");
            
            return (false, default);
        }
        catch (JsonException jsonException)
        {
            _logger.LogError(jsonException, "Failed to parse JSON response from API.");
            
            return (false, default);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Unexpected error in WebServiceStorage.");
            
            return (false, default);
        }
    }

    public Task SetValueAsync(T value) => Task.CompletedTask;
}