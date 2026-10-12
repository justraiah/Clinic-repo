
using System.Text.Json;
using Capstone_Clinic.Models.IoT;

namespace Capstone_Clinic.Services;

public class Esp32Service
{
    private readonly HttpClient _httpClient;
    private readonly string _sensorUrl;

    public Esp32Service(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        _sensorUrl = (
            configuration["Integration:Esp32BaseUrl"]
            ?? "http://10.67.173.55"
        ).TrimEnd('/');
    }

    public async Task<VitalReading?> GetVitalsAsync()
    {
        try
        {
            Console.WriteLine($"Requesting a new reading from ESP32: {_sensorUrl}");

            using var response = await _httpClient.GetAsync(_sensorUrl);

            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"ESP32 replied {(int)response.StatusCode}: {json}"
            );

            if (!response.IsSuccessStatusCode)
                return null;

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            bool fingerDetected =
                root.TryGetProperty("fingerDetected", out var finger)
                && finger.GetBoolean();

            if (root.TryGetProperty("success", out var success)
                && !success.GetBoolean())
            {
                return null;
            }

            if (!fingerDetected)
                return null;

            int heartRate = root.TryGetProperty("heartRate", out var hr)
                ? (int)Math.Round(hr.GetDouble())
                : 0;

            int spo2 = root.TryGetProperty("spo2", out var oxygen)
                ? oxygen.GetInt32()
                : 0;

            if (heartRate <= 0 || spo2 <= 0)
                return null;

            return new VitalReading
            {
                HeartRate = heartRate,
                AverageHeartRate = heartRate,
                Spo2 = spo2,
                FingerDetected = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ESP32 sensor request ERROR: {ex.Message}");
            return null;
        }
    }
}