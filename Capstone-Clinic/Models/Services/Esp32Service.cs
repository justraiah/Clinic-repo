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
        Console.WriteLine("1. Sending request...");

        var response = await _httpClient.GetAsync("http://192.168.1.16/");

        Console.WriteLine("2. Response received.");

        Console.WriteLine($"Status: {response.StatusCode}");

        Console.WriteLine("3. Reading body...");

        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine("4. Body read.");

        Console.WriteLine(json);

        Console.WriteLine("5. Deserializing...");

        var result = JsonSerializer.Deserialize<VitalReading>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Console.WriteLine("6. Finished.");

        return result;
    }
}