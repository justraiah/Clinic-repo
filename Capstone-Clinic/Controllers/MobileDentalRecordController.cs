
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/student/dental-records")]
[Authorize(AuthenticationSchemes = "MobileJwt")]
public class MobileDentalRecordController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileDentalRecordController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDentalRecords()
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

        var dentalRecords = await _context.DentalRecords
            .AsNoTracking()
            .Where(record =>
                record.ClinicPatient != null &&
                record.ClinicPatient.StudentId == studentId)
            .OrderByDescending(record => record.VisitDate)
            .Select(record => new
            {
                dentalRecordId = record.DentalRecordId,
                clinicPatientId = record.ClinicPatientId,
                visitDate = record.VisitDate,
                chiefComplaint = record.ChiefComplaint,
                dentalFindings = record.DentalFindings,
                diagnosis = record.Diagnosis,
                treatmentProvided = record.TreatmentProvided,
                medications = record.Medications,
                clinicalNotes = record.ClinicalNotes,
                createdAt = record.CreatedAt
            })
            .ToListAsync();

        return Ok(dentalRecords);
    }
}
