using System.Text.Json;
using Capstone_Clinic.Models.IoT;
using Microsoft.Extensions.Configuration;

namespace Capstone_Clinic.Services;

public class Esp32Service
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public Esp32Service(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public bool IsEsp32Online { get; private set; }
    public async Task<VitalReading?> GetVitalsAsync()
    {
        try
        {
            Console.WriteLine("Sending request to ESP32...");

            var response = await _httpClient.GetAsync(_configuration["Esp32:BaseUrl"]);

            Console.WriteLine($"ESP32 responded: {response.StatusCode}");

            IsEsp32Online = response.IsSuccessStatusCode;

            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response JSON: {json}");

            var result = JsonSerializer.Deserialize<VitalReading>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ESP32 ERROR: {ex}");

            IsEsp32Online = false;

            return null;
        }
    }
}