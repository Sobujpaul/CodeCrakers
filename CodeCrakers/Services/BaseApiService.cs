using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CodeCrakers.Services
{
    public abstract class BaseApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _baseUrl;

        protected BaseApiService(string baseUrl)
        {
            _baseUrl = baseUrl;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeCrakers/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Set timeout to 10 seconds
        }

        protected async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException($"HTTP request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new ApiException($"JSON parsing failed: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ApiException($"Request timeout: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new ApiException($"Unexpected error: {ex.Message}", ex);
            }
        }

        protected async Task<bool> TestConnectionAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}{endpoint}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
        public ApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}
