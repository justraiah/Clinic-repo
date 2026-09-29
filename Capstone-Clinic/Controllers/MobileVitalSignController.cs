using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/student/vital-signs")]
[Authorize(AuthenticationSchemes = "MobileJwt")]
public class MobileVitalSignController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileVitalSignController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVitalSigns()
    {
        var studentIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(studentIdClaim, out int studentId))
        {
            return Unauthorized(new
            {
                message = "Invalid student authentication."
            });
        }

        var vitalSigns = await _context.VitalSignLogs
            .Where(vs => _context.MedicalRecords
                .Any(mr =>
                    mr.MedicalRecordId == vs.MedicalRecordId &&
                    mr.StudentId == studentId))
            .OrderByDescending(vs => vs.RecordedAt)
            .Select(vs => new
            {
                vitalSignLogId = vs.VitalSignLogId,
                medicalRecordId = vs.MedicalRecordId,

                temperature = vs.Temperature,
                heartRate = vs.HeartRate,
                respiratoryRate = vs.RespiratoryRate,
                oxygenSaturation = vs.OxygenSaturation,

                systolicBP = vs.SystolicBP,
                diastolicBP = vs.DiastolicBP,

                visitReason = vs.VisitReason,
                recordedAt = vs.RecordedAt,

                status = vs.Status,
                remarks = vs.Remarks
            })
            .ToListAsync();

        return Ok(vitalSigns);
    }
}