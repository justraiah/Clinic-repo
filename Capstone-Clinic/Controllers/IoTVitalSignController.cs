
using Capstone_Clinic.Data;
using Capstone_Clinic.Models.IoT;
using Capstone_Clinic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Data;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/iot/vital-signs")]
public class IoTVitalSignController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IoTDevicePresence _presence;

    public IoTVitalSignController(
        AppDbContext context,
        IConfiguration configuration,
        IoTDevicePresence presence)
    {
        _context = context;
        _configuration = configuration;
        _presence = presence;
    }

    private bool IsValidIoTKey(string? iotKey)
    {
        var configuredKey = _configuration["IoT:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey) ||
            string.IsNullOrWhiteSpace(iotKey))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(iotKey);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        if (providedBytes.Length != configuredBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            providedBytes,
            configuredBytes);
    }

    // CMS: Create a measurement request.
    [Authorize]
    [HttpPost("requests")]
    public async Task<IActionResult> CreateMeasurementRequest()
    {
        var now = DateTime.UtcNow;

        var measurementRequest = new IoTMeasurementRequest
        {
            RequestId = Guid.NewGuid(),
            Status = "Pending",
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(3)
        };

        _context.IoTMeasurementRequests.Add(measurementRequest);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            requestId = measurementRequest.RequestId,
            status = measurementRequest.Status,
            expiresAt = measurementRequest.ExpiresAt
        });
    }

    // ESP32: Claim the next pending measurement request.
    [AllowAnonymous]
    [HttpGet("device/next")]
    public async Task<IActionResult> GetNextMeasurementRequest(
        [FromHeader(Name = "X-IoT-Key")] string? iotKey)
    {
        if (!IsValidIoTKey(iotKey))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid IoT credentials."
            });
        }

        _presence.MarkSeen();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var now = DateTime.UtcNow;

        var expiredRequests = await _context.IoTMeasurementRequests
            .Where(r => r.Status == "Pending" && r.ExpiresAt <= now)
            .ToListAsync();

        foreach (var expiredRequest in expiredRequests)
        {
            expiredRequest.Status = "Expired";
        }

        var measurementRequest =
            await _context.IoTMeasurementRequests
                .Where(r => r.Status == "Pending" &&
                            r.ExpiresAt > now)
                .OrderBy(r => r.CreatedAt)
                .FirstOrDefaultAsync();

        if (measurementRequest == null)
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return NoContent();
        }

        measurementRequest.Status = "Claimed";
        measurementRequest.ClaimedAt = now;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            success = true,
            requestId = measurementRequest.RequestId,
            expiresAt = measurementRequest.ExpiresAt
        });
    }

    // ESP32: Submit a reading for its claimed request.
    [AllowAnonymous]
    [HttpPost("device/result")]

    public async Task<IActionResult> ReceiveMeasurementResult(
    [FromBody] IoTVitalSignRequest? request,
    [FromHeader(Name = "X-IoT-Key")] string? iotKey)
    {
        if (!IsValidIoTKey(iotKey))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid IoT credentials."
            });
        }

        _presence.MarkSeen();

        if (request == null || request.RequestId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Invalid measurement request ID."
            });
        }

        var readingIsValid = request.FingerDetected &&
            request.HeartRate > 0 &&
            request.HeartRate <= 300 &&
            request.Spo2 > 0 &&
            request.Spo2 <= 100;

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var measurementRequest =
            await _context.IoTMeasurementRequests
                .FirstOrDefaultAsync(r =>
                    r.RequestId == request.RequestId);

        if (measurementRequest == null)
        {
            return NotFound(new
            {
                success = false,
                message = "Measurement request not found."
            });
        }

        // Handle retries of an already completed measurement.
        if (measurementRequest.Status == "Completed")
        {
            var existingReading =
                await _context.PendingVitalReadings
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == request.RequestId);

            if (existingReading == null)
            {
                return Conflict(new
                {
                    success = false,
                    message = "Measurement was completed, but its reading could not be found."
                });
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                success = true,
                duplicate = true,
                message = "Measurement was already received successfully.",
                requestId = request.RequestId,
                readingId = existingReading.PendingVitalReadingId,
                heartRate = existingReading.HeartRate,
                spo2 = existingReading.Spo2
            });
        }

        // Handle retries of an already failed measurement.
        if (measurementRequest.Status == "Failed")
        {
            await transaction.CommitAsync();

            return Ok(new
            {
                success = false,
                duplicate = true,
                requestId = request.RequestId,
                message = "The sensor did not obtain a valid reading."
            });
        }

        if (measurementRequest.Status != "Claimed")
        {
            return Conflict(new
            {
                success = false,
                message = "Measurement request is not in the claimed state."
            });
        }

        if (measurementRequest.ExpiresAt <= DateTime.UtcNow)
        {
            measurementRequest.Status = "Expired";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Conflict(new
            {
                success = false,
                message = "Measurement request has expired."
            });
        }

        if (!readingIsValid)
        {
            measurementRequest.Status = "Failed";
            measurementRequest.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                success = false,
                requestId = request.RequestId,
                message = "No valid finger reading was obtained."
            });
        }

        var pendingReading = new PendingVitalReading
        {
            RequestId = request.RequestId,
            HeartRate = request.HeartRate,
            Spo2 = request.Spo2,
            FingerDetected = request.FingerDetected,
            ReceivedAt = DateTime.UtcNow,
            IsUsed = false
        };

        measurementRequest.Status = "Completed";
        measurementRequest.CompletedAt = DateTime.UtcNow;

        _context.PendingVitalReadings.Add(pendingReading);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            success = true,
            duplicate = false,
            message = "Vital-sign reading received successfully.",
            requestId = request.RequestId,
            readingId = pendingReading.PendingVitalReadingId,
            heartRate = pendingReading.HeartRate,
            spo2 = pendingReading.Spo2
        });
    }

    // CMS: Retrieve a reading belonging to a specific request.
    [Authorize]
    [HttpGet("requests/{requestId:guid}/reading")]
    public async Task<IActionResult> GetReadingForRequest(
    Guid requestId)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var measurementRequest =
            await _context.IoTMeasurementRequests
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

        if (measurementRequest == null)
        {
            await transaction.CommitAsync();

            return NotFound(new
            {
                success = false,
                message = "Measurement request not found."
            });
        }

        if ((measurementRequest.Status == "Pending" ||
             measurementRequest.Status == "Claimed") &&
            measurementRequest.ExpiresAt <= DateTime.UtcNow)
        {
            measurementRequest.Status = "Expired";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Conflict(new
            {
                success = false,
                message = "Measurement request has expired."
            });
        }

        if (measurementRequest.Status == "Expired")
        {
            await transaction.CommitAsync();

            return Conflict(new
            {
                success = false,
                message = "Measurement request has expired."
            });
        }

        if (measurementRequest.Status == "Failed")
        {
            await transaction.CommitAsync();

            return Conflict(new
            {
                success = false,
                message = "The sensor did not obtain a valid reading."
            });
        }

        var reading = await _context.PendingVitalReadings
            .FirstOrDefaultAsync(r =>
                r.RequestId == requestId &&
                !r.IsUsed);

        if (reading == null)
        {
            await transaction.CommitAsync();

            return Accepted(new
            {
                success = false,
                requestId,
                status = measurementRequest.Status,
                message = "Waiting for the ESP32 measurement."
            });
        }

        reading.IsUsed = true;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            success = true,
            requestId,
            readingId = reading.PendingVitalReadingId,
            heartRate = reading.HeartRate,
            spo2 = reading.Spo2,
            fingerDetected = reading.FingerDetected,
            receivedAt = reading.ReceivedAt
        });
    }

    // LEGACY: Retained temporarily for compatibility.
    [HttpPost]
    public async Task<IActionResult> ReceiveVitalSigns(
        [FromBody] IoTVitalSignRequest request,
        [FromHeader(Name = "X-IoT-Key")] string? iotKey)
    {
        if (!IsValidIoTKey(iotKey))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Invalid IoT credentials."
            });
        }

        if (!request.FingerDetected ||
            request.HeartRate <= 0 ||
            request.HeartRate > 300 ||
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
            ReceivedAt = DateTime.UtcNow,
            IsUsed = false
        };

        _context.PendingVitalReadings.Add(pendingReading);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            readingId = pendingReading.PendingVitalReadingId,
            heartRate = pendingReading.HeartRate,
            spo2 = pendingReading.Spo2
        });
    }

    // LEGACY: Retained temporarily for compatibility.
    [Authorize]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingVitalReading()
    {
        var reading = await _context.PendingVitalReadings
            .Where(r => !r.IsUsed && r.RequestId == null)
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefaultAsync();

        if (reading == null)
        {
            return NotFound(new
            {
                success = false,
                message = "No pending sensor reading found."
            });
        }

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
    public Guid RequestId { get; set; }

    public int HeartRate { get; set; }

    public int Spo2 { get; set; }

    public bool FingerDetected { get; set; }
}
