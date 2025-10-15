using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using homemonitor.Models;

namespace homemonitor.Services;

public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _baseUrl = string.Empty;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

    }

    public void SetBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_baseUrl))
                return null;
            
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/dashboard/stats");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DashboardStats>(json);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"API call failed: {ex.Message}");
            return null;
            
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_baseUrl))
                return false;
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/dashboard/stats");
            return response.IsSuccessStatusCode;
        }
        catch 
        {
            return false;
        }
        
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}