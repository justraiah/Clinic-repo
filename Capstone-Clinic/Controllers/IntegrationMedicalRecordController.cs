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
    [HttpGet("{studentNumber}/vital-signs")]
    public async Task<IActionResult> GetStudentVitalSigns(
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

        var vitalSigns = await _context.VitalSignLogs
            .Where(vs => _context.MedicalRecords
                .Any(mr =>
                    mr.MedicalRecordId == vs.MedicalRecordId &&
                    mr.StudentId == student.StudentId))
            .OrderByDescending(vs => vs.RecordedAt)
            .Select(vs => new
            {
                vitalSignLogId = vs.VitalSignLogId,
                medicalRecordId = vs.MedicalRecordId,
                temperature = vs.Temperature,
                heartRate = vs.HeartRate,
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