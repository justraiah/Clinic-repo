using Capstone_Clinic.Data;
using Capstone_Clinic.Helpers;
using Capstone_Clinic.Models;
using Capstone_Clinic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class CreateModel : PageModel
{

    private readonly AppDbContext _context;
    private readonly Esp32Service _esp32Service;

    public CreateModel(
        AppDbContext context,
        Esp32Service esp32Service)
    {
        _context = context;
        _esp32Service = esp32Service;
    }

    public string StudentNumber { get; set; } = "";

    public string StudentName { get; set; } = "";

    private void LoadMedicalRecordStudent(int medicalRecordId)
    {
        var medicalRecord = _context.MedicalRecords
            .FirstOrDefault(m => m.MedicalRecordId == medicalRecordId);

        if (medicalRecord == null)
            return;

        // Student medical record
        if (medicalRecord.StudentId.HasValue)
        {
            var student = _context.Students
                .FirstOrDefault(s => s.StudentId == medicalRecord.StudentId.Value);

            if (student == null)
                return;

            StudentNumber = student.StudentNumber;
            StudentName = student.FullName;
            return;
        }

        // Community patient medical record
        var clinicPatient = _context.ClinicPatients
            .FirstOrDefault(cp => cp.ClinicPatientId == medicalRecord.ClinicPatientId);

        if (clinicPatient == null)
            return;

        StudentNumber = clinicPatient.Identifier ?? "";
        StudentName = clinicPatient.FullName ?? "";
    }

    public IActionResult OnGet(int? medicalRecordId)
    {
        if (medicalRecordId.HasValue)
        {
            VitalSignLog = new VitalSignLog
            {
                MedicalRecordId = medicalRecordId.Value,
                RecordedAt = DateTime.Now
            };

            LoadMedicalRecordStudent(medicalRecordId.Value);

            return Page();
        }

        // Fallback if opened directly
        StudentList = new SelectList(
            _context.Students
                .Select(s => new
                {
                    s.StudentId,
                    Display = s.StudentNumber + " - " + s.FullName
                })
                .ToList(),
            "StudentId",
            "Display"
        );

        VitalSignLog = new VitalSignLog
        {
            RecordedAt = DateTime.Now
        };
        return Page();
    }

    [BindProperty]
    public VitalSignLog VitalSignLog { get; set; } = default!;
    [BindProperty]
    public int SelectedStudentId { get; set; }
    public SelectList StudentList { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.


    public async Task<IActionResult> OnPostReadSensorAsync()
    {
        Console.WriteLine("===== READ SENSOR HANDLER START =====");
        var reading = await _esp32Service.GetVitalsAsync();

        // Always reload the student information before returning the page
        if (VitalSignLog.MedicalRecordId > 0)
        {
            LoadMedicalRecordStudent(VitalSignLog.MedicalRecordId);
        }

        if (reading != null && reading.FingerDetected)
        {
            VitalSignLog.HeartRate =
                reading.AverageHeartRate > 0
                    ? reading.AverageHeartRate
                    : (int)Math.Round(reading.HeartRate);

            VitalSignLog.OxygenSaturation = reading.Spo2;

            ModelState.Remove("VitalSignLog.HeartRate");
            ModelState.Remove("VitalSignLog.OxygenSaturation");

            TempData["SuccessMessage"] =
                "Heart Rate and SpO₂ successfully read from ESP32.";
        }
        else
        {
            TempData["ErrorMessage"] =
                "Unable to obtain enough stable readings. Please keep your finger steady and try again.";
        }

        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine("===== SAVE HANDLER =====");
        if (!ModelState.IsValid)
        {
            StudentList = new SelectList(
                _context.Students
                    .Select(s => new
                    {
                        s.StudentId,
                        Display = s.StudentNumber + " - " + s.FullName
                    })
                    .ToList(),
                "StudentId",
                "Display"
            );

            return Page();
        }

        if (VitalSignLog.MedicalRecordId == 0)
        {
            ModelState.AddModelError("", "Medical Record not found.");

            StudentList = new SelectList(
                _context.Students
                    .Select(s => new
                    {
                        s.StudentId,
                        Display = s.StudentNumber + " - " + s.FullName
                    })
                    .ToList(),
                "StudentId",
                "Display"
            );

            return Page();
        }
        var medicalRecord = await _context.MedicalRecords
    .FirstOrDefaultAsync(m =>
        m.MedicalRecordId == VitalSignLog.MedicalRecordId);

        if (medicalRecord == null)
        {
            ModelState.AddModelError("", "Medical Record not found.");

            return Page();
        }

        VitalSignLog.ClinicPatientId = medicalRecord.ClinicPatientId;
        VitalSignLog.StaffId = 1;
        VitalSignLog.RecordedAt = DateTime.Now;

        // Evaluate the vital-sign readings
        VitalSignEvaluator.Evaluate(VitalSignLog);

        // Save the vital-sign record first
        _context.VitalSignLogs.Add(VitalSignLog);
        await _context.SaveChangesAsync();

        // Create an alert only for Warning or Critical readings
        if (VitalSignLog.Status == "Warning" ||
            VitalSignLog.Status == "Critical")
        {
            var firstRemark = VitalSignLog.Remarks
    .Split(',', StringSplitOptions.RemoveEmptyEntries)
    .FirstOrDefault()?.Trim() ?? "Abnormal Vital Signs";

            var alert = new Alert
            {
                VitalLogId = VitalSignLog.VitalSignLogId,

                AlertType = firstRemark,

                AlertMessage = VitalSignLog.Remarks,

                Status = "Active",

                CreatedAt = DateTime.Now
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();
        }
        Console.WriteLine("===== SAVE HANDLER END =====");
        return RedirectToPage("/MedicalRecordPages/Details",
    new { id = VitalSignLog.MedicalRecordId });
    }
}
