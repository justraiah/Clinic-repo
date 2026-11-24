
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Capstone_Clinic.Controllers;

[ApiController]
[Route("api/integration/community")]
public class IntegrationCommunityController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public IntegrationCommunityController(
        AppDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private bool IsAuthorized(string? integrationKey)
    {
        var configuredKey = _configuration["Integration:ApiKey"];

        if (string.IsNullOrWhiteSpace(configuredKey) ||
            string.IsNullOrWhiteSpace(integrationKey))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(integrationKey),
            Encoding.UTF8.GetBytes(configuredKey));
    }

    private async Task<ClinicPatient?> FindPatient(string identifier)
    {
        return await _context.ClinicPatients
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Identifier == identifier &&
                (p.PatientType == "Student" || p.PatientType == "Faculty"));
    }

    // MEDICAL RECORDS
    [HttpGet("{identifier}/medical-records")]
    public async Task<IActionResult> MedicalRecords(
        string identifier,
        [FromHeader(Name = "X-Integration-Key")] string? key)
    {
        if (!IsAuthorized(key))
            return Unauthorized(new { message = "Invalid integration credentials." });

        var patient = await FindPatient(identifier);

        if (patient == null)
            return NotFound(new { message = "Community patient not found." });

        var records = await _context.MedicalRecords
            .AsNoTracking()
            .Where(m => m.ClinicPatientId == patient.ClinicPatientId)
            .OrderByDescending(m => m.VisitDate)
            .Select(m => new
            {
                medicalRecordId = m.MedicalRecordId,
                visitDate = m.VisitDate,
                chiefComplaint = m.ChiefComplaint,
                painScale = m.PainScale,
                diagnosis = m.Diagnosis,
                medications = m.Medications,
                allergies = m.Allergies,
                clinicalNotes = m.ClinicalNotes,
                createdAt = m.CreatedAt,
                updatedAt = m.UpdatedAt
            })
            .ToListAsync();

        return Ok(records);
    }

    // VITAL SIGNS
    [HttpGet("{identifier}/vital-signs")]
    public async Task<IActionResult> VitalSigns(
        string identifier,
        [FromHeader(Name = "X-Integration-Key")] string? key)
    {
        if (!IsAuthorized(key))
            return Unauthorized(new { message = "Invalid integration credentials." });

        var patient = await FindPatient(identifier);

        if (patient == null)
            return NotFound(new { message = "Community patient not found." });

        var records = await _context.VitalSignLogs
            .AsNoTracking()
            .Where(v => _context.MedicalRecords.Any(m =>
                m.MedicalRecordId == v.MedicalRecordId &&
                m.ClinicPatientId == patient.ClinicPatientId))
            .OrderByDescending(v => v.RecordedAt)
            .Select(v => new
            {
                vitalSignLogId = v.VitalSignLogId,
                medicalRecordId = v.MedicalRecordId,
                temperature = v.Temperature,
                heartRate = v.HeartRate,
                respiratoryRate = v.RespiratoryRate,
                oxygenSaturation = v.OxygenSaturation,
                systolicBP = v.SystolicBP,
                diastolicBP = v.DiastolicBP,
                visitReason = v.VisitReason,
                recordedAt = v.RecordedAt,
                status = v.Status,
                remarks = v.Remarks
            })
            .ToListAsync();

        return Ok(records);
    }

    // DENTAL RECORDS
    [HttpGet("{identifier}/dental-records")]
    public async Task<IActionResult> DentalRecords(
        string identifier,
        [FromHeader(Name = "X-Integration-Key")] string? key)
    {
        if (!IsAuthorized(key))
            return Unauthorized(new { message = "Invalid integration credentials." });

        var patient = await FindPatient(identifier);

        if (patient == null)
            return NotFound(new { message = "Community patient not found." });

        var records = await _context.DentalRecords
            .AsNoTracking()
            .Where(r => r.ClinicPatientId == patient.ClinicPatientId)
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

    // REFERRALS
    [HttpGet("{identifier}/referrals")]
    public async Task<IActionResult> Referrals(
        string identifier,
        [FromHeader(Name = "X-Integration-Key")] string? key)
    {
        if (!IsAuthorized(key))
            return Unauthorized(new { message = "Invalid integration credentials." });

        var patient = await FindPatient(identifier);

        if (patient == null)
            return NotFound(new { message = "Community patient not found." });

        var records = await _context.Referrals
            .AsNoTracking()
            .Where(r => r.ClinicPatientId == patient.ClinicPatientId)
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

        return Ok(records);
    }
}
