using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/iot/vital-signs")]

public class IoTVitalSignController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public IoTVitalSignController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    [HttpPost]
    public IActionResult ReceiveVitalSigns(
    [FromBody] IoTVitalSignRequest request,
    [FromHeader(Name = "X-IoT-Key")] string? iotKey)
    {
        var configuredKey = _configuration["IoT:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey) ||
            string.IsNullOrWhiteSpace(iotKey) ||
            !string.Equals(
                iotKey,
                configuredKey,
                StringComparison.Ordinal))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid IoT credentials."
            });
        }
        if (!request.FingerDetected)
        {
            return BadRequest(new
            {
                success = false,
                message = "No finger detected."
            });
        }

        if (request.HeartRate <= 0 ||
            request.Spo2 <= 0)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid vital-sign reading."
            });
        }

        Console.WriteLine("===== ESP32 VITAL READING RECEIVED =====");
        Console.WriteLine($"Heart Rate: {request.HeartRate}");
        Console.WriteLine($"SpO2: {request.Spo2}");
        Console.WriteLine($"Finger Detected: {request.FingerDetected}");
        Console.WriteLine("=========================================");

        return Ok(new
        {
            success = true,
            message = "Vital-sign reading received successfully.",
            heartRate = request.HeartRate,
            spo2 = request.Spo2
        });
    }
}

public class IoTVitalSignRequest
{
    public int HeartRate { get; set; }

    public int Spo2 { get; set; }

    public bool FingerDetected { get; set; }
}