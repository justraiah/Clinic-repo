using Microsoft.AspNetCore.Mvc;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/iot/vital-signs")]
public class IoTVitalSignController : ControllerBase
{
    [HttpPost]
    public IActionResult ReceiveVitalSigns(
        [FromBody] IoTVitalSignRequest request)
    {
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