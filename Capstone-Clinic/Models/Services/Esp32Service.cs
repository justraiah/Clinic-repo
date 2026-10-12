using System.Text.Json;
using Capstone_Clinic.Models.IoT;
namespace Capstone_Clinic.Services;

// Webserver mode: the CMS asks the ESP32 directly (GET http://<esp32-ip>/)
// and the ESP32 measures and answers with {"heartRate":..,"spo2":..,"fingerDetected":..}
public class Esp32Service
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public Esp32Service(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = configuration["Esp32:BaseUrl"] ?? "http://192.168.1.16/";
    }

    public async Task<VitalReading?> GetVitalsAsync()
    {
        try
        {
            Console.WriteLine($"Requesting vitals from ESP32: {_baseUrl}");

            var response = await _httpClient.GetAsync(_baseUrl);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"ESP32 replied {(int)response.StatusCode}: {json}");

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<VitalReading>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ESP32 request ERROR: {ex.Message}");
            return null;
        }
    }
}