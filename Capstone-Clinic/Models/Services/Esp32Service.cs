using System.Text.Json;
using Capstone_Clinic.Models.IoT;

namespace Capstone_Clinic.Services;

public class Esp32Service
{
    private readonly HttpClient _httpClient;

    public Esp32Service(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<VitalReading?> GetVitalsAsync()
    {
        var response = await _httpClient.GetAsync("http://192.168.1.16/");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<VitalReading>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }
}