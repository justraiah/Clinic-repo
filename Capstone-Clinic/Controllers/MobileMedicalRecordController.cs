using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/mobile/student/medical-records")]
[Authorize(AuthenticationSchemes = "MobileJwt")]
public class MobileMedicalRecordController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileMedicalRecordController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyMedicalRecords()
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

        var medicalRecords = await _context.MedicalRecords
            .Where(mr => mr.StudentId == studentId)
            .OrderByDescending(mr => mr.VisitDate)
            .Select(mr => new
            {
                medicalRecordId = mr.MedicalRecordId,
                visitDate = mr.VisitDate,

                chiefComplaint = mr.ChiefComplaint,
                painScale = mr.PainScale,
                diagnosis = mr.Diagnosis,
                medications = mr.Medications,
                allergies = mr.Allergies,
                clinicalNotes = mr.ClinicalNotes,

                createdAt = mr.CreatedAt,
                updatedAt = mr.UpdatedAt
            })
            .ToListAsync();

        return Ok(medicalRecords);
    }
}