using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class ReassessmentModel : PageModel
{
    private readonly AppDbContext _context;

    public ReassessmentModel(AppDbContext context)
    {
        _context = context;
    }

    public int VitalSignLogId { get; set; }
    public int MedicalRecordId { get; set; }
    public int ReassessmentNumber { get; set; }

    public string PatientName { get; set; } = "";

    // Original readings (read-only)
    public double Temperature { get; set; }
    public int HeartRate { get; set; }
    public int RespiratoryRate { get; set; }
    public int OxygenSaturation { get; set; }
    public int SystolicBP { get; set; }
    public int DiastolicBP { get; set; }
    public DateTime RecordedAt { get; set; }

    [BindProperty]
    public VitalSignReassessment Reassessment { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var vital = await LoadVitalAsync(id);

        if (vital == null)
            return NotFound();

        await LoadPageDataAsync(vital);

        Reassessment = new VitalSignReassessment();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var vital = await LoadVitalAsync(id);

        if (vital == null)
            return NotFound();

        await LoadPageDataAsync(vital);

        if (!ModelState.IsValid)
            return Page();

        // Generate the reassessment sequence on the server.
        var lastNumber = await _context.VitalSignReassessments
            .Where(r => r.VitalSignLogId == vital.VitalSignLogId)
            .Select(r => (int?)r.ReassessmentNumber)
            .MaxAsync() ?? 0;

        Reassessment.VitalSignLogId = vital.VitalSignLogId;
        Reassessment.ReassessmentNumber = lastNumber + 1;
        Reassessment.RecordedAt = Capstone_Clinic.Helpers.TimeHelper.Now;
        Reassessment.StaffId = 1;

        // Evaluate this reassessment independently.
        EvaluateReassessment(Reassessment);

        _context.VitalSignReassessments.Add(Reassessment);

        // Create an alert for abnormal reassessment readings.
        // Existing alerts are NOT automatically resolved.
        if (Reassessment.Status == "Warning" ||
            Reassessment.Status == "Critical")
        {
            var alert = new Alert
            {
                VitalLogId = vital.VitalSignLogId,
                AlertType = Reassessment.Status,
                AlertMessage =
                    $"Reassessment #{Reassessment.ReassessmentNumber} " +
                    $"({Reassessment.RecordedAt:MMM dd, yyyy hh:mm tt}): " +
                    $"{Reassessment.Remarks}. " +
                    $"Temp: {Reassessment.Temperature:F1} °C, " +
                    $"HR: {Reassessment.HeartRate} BPM, " +
                    $"RR: {Reassessment.RespiratoryRate}/min, " +
                    $"SpO₂: {Reassessment.OxygenSaturation}%, " +
                    $"BP: {Reassessment.SystolicBP}/{Reassessment.DiastolicBP}.",
                Status = "Active",
                CreatedAt = Capstone_Clinic.Helpers.TimeHelper.Now
            };

            _context.Alerts.Add(alert);
        }

        await _context.SaveChangesAsync();

        if (Reassessment.Status == "Critical")
        {
            TempData["CriticalAlertMessage"] =
                $"Critical readings detected during reassessment #{Reassessment.ReassessmentNumber}. " +
                "Please review the patient immediately. " +
                Reassessment.Remarks;
        }

        return RedirectToPage("./Index", new
        {
            medicalRecordId = vital.MedicalRecordId
        });
    }

    private async Task<VitalSignLog?> LoadVitalAsync(int id)
    {
        return await _context.VitalSignLogs
            .FirstOrDefaultAsync(v => v.VitalSignLogId == id);
    }

    private async Task LoadPageDataAsync(VitalSignLog vital)
    {
        VitalSignLogId = vital.VitalSignLogId;
        MedicalRecordId = vital.MedicalRecordId;

        Temperature = vital.Temperature;
        HeartRate = vital.HeartRate;
        RespiratoryRate = vital.RespiratoryRate;
        OxygenSaturation = vital.OxygenSaturation;
        SystolicBP = vital.SystolicBP;
        DiastolicBP = vital.DiastolicBP;
        RecordedAt = vital.RecordedAt;

        ReassessmentNumber = (await _context.VitalSignReassessments
            .Where(r => r.VitalSignLogId == vital.VitalSignLogId)
            .Select(r => (int?)r.ReassessmentNumber)
            .MaxAsync() ?? 0) + 1;

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(m => m.MedicalRecordId == vital.MedicalRecordId);

        if (medicalRecord?.StudentId != null)
        {
            PatientName = await _context.Students
                .Where(s => s.StudentId == medicalRecord.StudentId)
                .Select(s => s.FullName)
                .FirstOrDefaultAsync() ?? "Unknown Patient";
        }
        else if (medicalRecord?.ClinicPatientId != null)
        {
            PatientName = await _context.ClinicPatients
                .Where(p => p.ClinicPatientId == medicalRecord.ClinicPatientId)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync() ?? "Unknown Patient";
        }
    }

    private static void EvaluateReassessment(VitalSignReassessment r)
    {
        var findings = new List<string>();
        string status = "Normal";

        void SetStatus(string newStatus)
        {
            if (newStatus == "Critical")
                status = "Critical";
            else if (newStatus == "Warning" && status != "Critical")
                status = "Warning";
        }

        // Temperature
        if (r.Temperature > 39)
        {
            findings.Add("Hyperpyrexia");
            SetStatus("Critical");
        }
        else if (r.Temperature > 38)
        {
            findings.Add("Fever");
            SetStatus("Warning");
        }
        else if (r.Temperature < 34)
        {
            findings.Add("Severe Hypothermia");
            SetStatus("Critical");
        }
        else if (r.Temperature < 35.5)
        {
            findings.Add("Hypothermia");
            SetStatus("Warning");
        }

        // Heart Rate
        if (r.HeartRate > 120)
        {
            findings.Add("Severe Tachycardia");
            SetStatus("Critical");
        }
        else if (r.HeartRate > 110)
        {
            findings.Add("Tachycardia");
            SetStatus("Warning");
        }
        else if (r.HeartRate < 50)
        {
            findings.Add("Severe Bradycardia");
            SetStatus("Critical");
        }
        else if (r.HeartRate < 60)
        {
            findings.Add("Bradycardia");
            SetStatus("Warning");
        }

        // Respiratory Rate
        if (r.RespiratoryRate < 8)
        {
            findings.Add("Severe Bradypnea");
            SetStatus("Critical");
        }
        else if (r.RespiratoryRate < 12)
        {
            findings.Add("Bradypnea");
            SetStatus("Warning");
        }
        else if (r.RespiratoryRate > 30)
        {
            findings.Add("Severe Tachypnea");
            SetStatus("Critical");
        }
        else if (r.RespiratoryRate > 25)
        {
            findings.Add("Tachypnea");
            SetStatus("Warning");
        }

        // Oxygen Saturation
        if (r.OxygenSaturation < 90)
        {
            findings.Add("Severe Hypoxemia");
            SetStatus("Critical");
        }
        else if (r.OxygenSaturation < 95)
        {
            findings.Add("Hypoxemia");
            SetStatus("Warning");
        }

        // Systolic Blood Pressure
        if (r.SystolicBP >= 150)
        {
            findings.Add("Severe Hypertension (Systolic)");
            SetStatus("Critical");
        }
        else if (r.SystolicBP > 130)
        {
            findings.Add("Hypertension (Systolic)");
            SetStatus("Warning");
        }
        else if (r.SystolicBP < 80)
        {
            findings.Add("Severe Hypotension (Systolic)");
            SetStatus("Critical");
        }
        else if (r.SystolicBP < 90)
        {
            findings.Add("Hypotension (Systolic)");
            SetStatus("Warning");
        }

        // Diastolic Blood Pressure
        if (r.DiastolicBP >= 100)
        {
            findings.Add("Severe Hypertension (Diastolic)");
            SetStatus("Critical");
        }
        else if (r.DiastolicBP > 90)
        {
            findings.Add("Hypertension (Diastolic)");
            SetStatus("Warning");
        }
        else if (r.DiastolicBP < 50)
        {
            findings.Add("Severe Hypotension (Diastolic)");
            SetStatus("Critical");
        }
        else if (r.DiastolicBP < 60)
        {
            findings.Add("Hypotension (Diastolic)");
            SetStatus("Warning");
        }

        r.Status = status;
        r.Remarks = findings.Any()
            ? string.Join(", ", findings)
            : "No threshold abnormalities detected";
    }
}