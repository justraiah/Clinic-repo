using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/integration/student")]
public class IntegrationMedicalRecordController : ControllerBase
{
    private readonly AppDbContext _context;

    public IntegrationMedicalRecordController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{studentNumber}/medical-records")]
    public async Task<IActionResult> GetStudentMedicalRecords(
        string studentNumber)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s =>
                s.StudentNumber == studentNumber);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var medicalRecords = await _context.MedicalRecords
            .Where(mr => mr.StudentId == student.StudentId)
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