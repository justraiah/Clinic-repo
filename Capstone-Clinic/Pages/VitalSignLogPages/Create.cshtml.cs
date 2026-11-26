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
    private readonly IoTDevicePresence _devicePresence;

    public CreateModel(
        AppDbContext context,
        IoTDevicePresence devicePresence)
    {
        _context = context;
        _devicePresence = devicePresence;
    }

    public string StudentNumber { get; set; } = "";

    public string StudentName { get; set; } = "";

    public string IoTDeviceStatus { get; set; } = "Checking...";

    [BindProperty]
    public VitalSignLog VitalSignLog { get; set; } = default!;

    [BindProperty]
    public int SelectedClinicPatientId { get; set; }

    public SelectList ClinicPatientList { get; set; } = default!;

    private void LoadMedicalRecordPatient(int medicalRecordId)
    {
        var medicalRecord = _context.MedicalRecords
            .FirstOrDefault(m => m.MedicalRecordId == medicalRecordId);

        if (medicalRecord == null)
            return;

        var clinicPatient = _context.ClinicPatients
            .FirstOrDefault(cp => cp.ClinicPatientId == medicalRecord.ClinicPatientId);

        if (clinicPatient == null)
            return;

        StudentNumber = clinicPatient.Identifier ?? "";
        StudentName = clinicPatient.FullName ?? "";
    }

    private void LoadClinicPatientList()
    {
        var patients = _context.ClinicPatients
            .AsEnumerable()
            .OrderBy(cp => cp.FullName)
            .Select(cp => new
            {
                cp.ClinicPatientId,
                Display = $"{cp.Identifier ?? "No ID"} - " +
                          $"{cp.FullName ?? "Unnamed Patient"} " +
                          $"({cp.PatientType ?? "Patient"})"
            })
            .ToList();

        ClinicPatientList = new SelectList(
            patients,
            nameof(ClinicPatient.ClinicPatientId),
            "Display"
        );
    }

    private void PrepareStandaloneForm()
    {
        LoadClinicPatientList();
    }

    public async Task<IActionResult> OnGetAsync(int? medicalRecordId)
    {
        await CheckIoTDeviceStatusAsync();

        VitalSignLog = new VitalSignLog
        {
            MedicalRecordId = medicalRecordId,
            RecordedAt = TimeHelper.Now
        };

        if (medicalRecordId.HasValue)
        {
            LoadMedicalRecordPatient(medicalRecordId.Value);
            return Page();
        }

        // Standalone vital-sign recording
        PrepareStandaloneForm();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CheckIoTDeviceStatusAsync();

        MedicalRecord? medicalRecord = null;
        ClinicPatient? clinicPatient = null;

        // Consultation-linked recording
        if (VitalSignLog.MedicalRecordId.HasValue)
        {
            medicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m =>
                    m.MedicalRecordId == VitalSignLog.MedicalRecordId.Value);

            if (medicalRecord == null)
            {
                ModelState.AddModelError(
                    "",
                    "Medical Record not found.");

                VitalSignLog.MedicalRecordId = null;
            }
            else
            {
                clinicPatient = await _context.ClinicPatients
                    .FirstOrDefaultAsync(cp =>
                        cp.ClinicPatientId == medicalRecord.ClinicPatientId);

                if (clinicPatient == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The patient associated with this Medical Record could not be found.");
                }
            }
        }
        // Standalone recording
        else
        {
            if (SelectedClinicPatientId <= 0)
            {
                ModelState.AddModelError(
                    nameof(SelectedClinicPatientId),
                    "Please select a patient.");
            }
            else
            {
                clinicPatient = await _context.ClinicPatients
                    .FirstOrDefaultAsync(cp =>
                        cp.ClinicPatientId == SelectedClinicPatientId);

                if (clinicPatient == null)
                {
                    ModelState.AddModelError(
                        nameof(SelectedClinicPatientId),
                        "Selected patient not found.");
                }
            }
        }

        // Return the form if validation failed
        if (!ModelState.IsValid)
        {
            if (VitalSignLog.MedicalRecordId.HasValue)
            {
                LoadMedicalRecordPatient(
                    VitalSignLog.MedicalRecordId.Value);
            }
            else
            {
                PrepareStandaloneForm();
            }

            return Page();
        }

        // Assign the patient directly, regardless of whether
        // the vital-sign record is consultation-linked.
        if (clinicPatient == null)
        {
            ModelState.AddModelError("", "Patient could not be identified.");

            PrepareStandaloneForm();
            return Page();
        }

        VitalSignLog.ClinicPatientId = clinicPatient.ClinicPatientId;
        VitalSignLog.StaffId = 1;
        VitalSignLog.RecordedAt = TimeHelper.Now;

        // Monitoring is still required
        if (VitalSignLog.TimeIn == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Please start patient monitoring before saving vital signs.");

            if (VitalSignLog.MedicalRecordId.HasValue)
            {
                LoadMedicalRecordPatient(
                    VitalSignLog.MedicalRecordId.Value);
            }
            else
            {
                PrepareStandaloneForm();
            }

            return Page();
        }

        // Evaluate the vital-sign readings
        VitalSignEvaluator.Evaluate(VitalSignLog);

        // Add the vital-sign record
        _context.VitalSignLogs.Add(VitalSignLog);

        // Create an alert in the same save operation
        if (VitalSignLog.Status == "Warning" ||
            VitalSignLog.Status == "Critical")
        {
            var firstRemark = VitalSignLog.Remarks
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim() ?? "Abnormal Vital Signs";

            var alert = new Alert
            {
                VitalSignLog = VitalSignLog,
                AlertType = firstRemark,
                AlertMessage = VitalSignLog.Remarks,
                Status = "Active",
                CreatedAt = TimeHelper.Now
            };

            _context.Alerts.Add(alert);
        }

        await _context.SaveChangesAsync();

        // Immediate Critical alert message
        if (VitalSignLog.Status == "Critical")
        {
            var patientName = clinicPatient.FullName ?? "patient";

            TempData["CriticalAlertMessage"] =
                $"Critical vital signs detected for {patientName}. " +
                VitalSignLog.Remarks;
        }

        // Return to consultation details if consultation-linked;
        // otherwise return to the general Vital Signs page.
        if (VitalSignLog.MedicalRecordId.HasValue)
        {
            return RedirectToPage(
                "/MedicalRecordPages/Details",
                new { id = VitalSignLog.MedicalRecordId.Value });
        }

        TempData["SuccessMessage"] = "Vital signs recorded successfully.";

        return RedirectToPage("/VitalSignLogPages/Index");
    }

    private Task CheckIoTDeviceStatusAsync()
    {
        IoTDeviceStatus = _devicePresence.IsOnline()
            ? "Connected"
            : "Offline";

        return Task.CompletedTask;
    }
}