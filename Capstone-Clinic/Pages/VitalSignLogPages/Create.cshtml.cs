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

    public IActionResult OnGet(int? medicalRecordId)
    {
        if (medicalRecordId.HasValue)
        {
            VitalSignLog = new VitalSignLog
            {
                MedicalRecordId = medicalRecordId.Value,
                RecordedAt = DateTime.Now
            };

            var medicalRecord = _context.MedicalRecords
                .FirstOrDefault(m => m.MedicalRecordId == medicalRecordId.Value);

            if (medicalRecord != null)
            {
                var student = _context.Students
                    .FirstOrDefault(s => s.StudentId == medicalRecord.StudentId);

                if (student != null)
                {
                    StudentNumber = student.StudentNumber;
                    StudentName = student.FullName;
                }
            }

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
        var reading = await _esp32Service.GetVitalsAsync();

        if (VitalSignLog.MedicalRecordId != 0)
        {
            var medicalRecord = _context.MedicalRecords
                .FirstOrDefault(m => m.MedicalRecordId == VitalSignLog.MedicalRecordId);

            if (medicalRecord != null)
            {
                var student = _context.Students
                    .FirstOrDefault(s => s.StudentId == medicalRecord.StudentId);

                if (student != null)
                {
                    StudentNumber = student.StudentNumber;
                    StudentName = student.FullName;
                }
            }
        }
        if (reading != null && reading.FingerDetected)
        {
            VitalSignLog.HeartRate = reading.AverageHeartRate;

            ModelState.Remove("VitalSignLog.HeartRate");

            TempData["SuccessMessage"] =
                "Heart rate successfully read from ESP32.";
        }
        else
        {
            TempData["ErrorMessage"] =
                "No finger detected on the sensor.";
        }

        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
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

        return RedirectToPage("/MedicalRecordPages/Details",
    new { id = VitalSignLog.MedicalRecordId });
    }
}
