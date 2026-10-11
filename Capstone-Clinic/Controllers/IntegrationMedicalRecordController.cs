using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/integration/student")]
public class IntegrationMedicalRecordController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public IntegrationMedicalRecordController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("{studentNumber}/medical-records")]
    public async Task<IActionResult> GetStudentMedicalRecords(
    string studentNumber,
    [FromHeader(Name = "X-Integration-Key")] string? integrationKey)
    {


        if (!IsAuthorizedIntegrationRequest(integrationKey))
        {
            return Unauthorized(new
            {
                message = "Invalid integration credentials."
            });
        }

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
    string studentNumber,
    [FromHeader(Name = "X-Integration-Key")] string? integrationKey)
    {
        if (!IsAuthorizedIntegrationRequest(integrationKey))
        {
            return Unauthorized(new
            {
                message = "Invalid integration credentials."
            });
        }

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


    [HttpGet("{studentNumber}/dental-records")]
    public async Task<IActionResult> GetStudentDentalRecords(
        string studentNumber,
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey)
    {
        if (!IsAuthorizedIntegrationRequest(integrationKey))
        {
            return Unauthorized(new
            {
                message = "Invalid integration credentials."
            });
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

        if (student == null)
        {
            return NotFound(new { message = "Student not found." });
        }

        var records = await _context.DentalRecords
            .AsNoTracking()
            .Where(r => r.ClinicPatient != null &&
                        r.ClinicPatient.StudentId == student.StudentId)
            .OrderByDescending(r => r.VisitDate)
            .Select(r => new
            {
                dentalRecordId = r.DentalRecordId,
                clinicPatientId = r.ClinicPatientId,
                visitDate = r.VisitDate,
                chiefComplaint = r.ChiefComplaint,
                dentalFindings = r.DentalFindings,
                diagnosis = r.Diagnosis,
                treatmentProvided = r.TreatmentProvided,
                medications = r.Medications,
                clinicalNotes = r.ClinicalNotes,
                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("{studentNumber}/referrals")]
    public async Task<IActionResult> GetStudentReferrals(
        string studentNumber,
        [FromHeader(Name = "X-Integration-Key")] string? integrationKey)
    {
        if (!IsAuthorizedIntegrationRequest(integrationKey))
        {
            return Unauthorized(new
            {
                message = "Invalid integration credentials."
            });
        }

        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

        if (student == null)
        {
            return NotFound(new { message = "Student not found." });
        }

        var referrals = await _context.Referrals
            .AsNoTracking()
            .Where(r => r.ClinicPatient != null &&
                        r.ClinicPatient.StudentId == student.StudentId)
            .OrderByDescending(r => r.ReferralDate)
            .Select(r => new
            {
                referralId = r.ReferralId,
                clinicPatientId = r.ClinicPatientId,
                referralDate = r.ReferralDate,
                facilityName = r.FacilityName,
                reasonForReferral = r.ReasonForReferral,
                clinicalNotes = r.ClinicalNotes,
                status = r.Status,
                createdAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(referrals);
    }

    private bool IsAuthorizedIntegrationRequest(string? integrationKey)
    {
        var configuredKey =
            _configuration["Integration:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey))
            return false;

        if (string.IsNullOrWhiteSpace(integrationKey))
            return false;

        return string.Equals(
            integrationKey,
            configuredKey,
            StringComparison.Ordinal);
    }
}