
using Capstone_Clinic.Data;
using Capstone_Clinic.Models.IoT;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/iot/vital-signs")]
public class IoTVitalSignController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public IoTVitalSignController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveVitalSigns(
        [FromBody] IoTVitalSignRequest request,
        [FromHeader(Name = "X-IoT-Key")] string? iotKey)
    {
        var configuredKey = _configuration["IoT:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            configuredKey = _configuration["Integration:ApiKey"];

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
            request.Spo2 <= 0 ||
            request.Spo2 > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid vital-sign reading."
            });
        }

        var pendingReading = new PendingVitalReading
        {
            HeartRate = request.HeartRate,
            Spo2 = request.Spo2,
            FingerDetected = request.FingerDetected,
            ReceivedAt = DateTime.Now,
            IsUsed = false
        };

        _context.PendingVitalReadings.Add(pendingReading);
        await _context.SaveChangesAsync();

        Console.WriteLine("===== ESP32 VITAL READING SAVED =====");
        Console.WriteLine($"Reading ID: {pendingReading.PendingVitalReadingId}");
        Console.WriteLine($"Heart Rate: {pendingReading.HeartRate}");
        Console.WriteLine($"SpO2: {pendingReading.Spo2}");
        Console.WriteLine($"Received At: {pendingReading.ReceivedAt}");
        Console.WriteLine("=====================================");

        return Ok(new
        {
            success = true,
            message = "Vital-sign reading saved successfully.",
            readingId = pendingReading.PendingVitalReadingId,
            heartRate = pendingReading.HeartRate,
            spo2 = pendingReading.Spo2,
            receivedAt = pendingReading.ReceivedAt
        });
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingVitalReading()
    {
        var reading = await _context.PendingVitalReadings
            .Where(r => !r.IsUsed)
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefaultAsync();

        if (reading == null)
        {
            return NotFound(new
            {
                success = false,
                message = "No pending sensor reading found. Open the ESP32 page first."
            });
        }

        // Consume the reading so repeated button presses cannot reuse stale data.
        reading.IsUsed = true;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            readingId = reading.PendingVitalReadingId,
            heartRate = reading.HeartRate,
            spo2 = reading.Spo2,
            fingerDetected = reading.FingerDetected,
            receivedAt = reading.ReceivedAt
        });
    }

}

public class IoTVitalSignRequest
{
    public int HeartRate { get; set; }

    public int Spo2 { get; set; }

    public bool FingerDetected { get; set; }
}
