using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;
using Capstone_Clinic.Helpers;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public VitalSignLog VitalSignLog { get; set; } = default!;
    [BindProperty(SupportsGet = true)]
    public int? MedicalRecordId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? medicalRecordId)
    {
        MedicalRecordId = medicalRecordId;
        if (id == null)
        {
            return NotFound();
        }

        var vitalsignlog = await _context.VitalSignLogs.FirstOrDefaultAsync(m => m.VitalSignLogId == id);
        if (vitalsignlog is null)
        {
            return NotFound();
        }
        VitalSignLog = vitalsignlog;
        return Page();
    }

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var existingVital = await _context.VitalSignLogs
            .FirstOrDefaultAsync(v =>
                v.VitalSignLogId == VitalSignLog.VitalSignLogId);

        if (existingVital == null)
        {
            return NotFound();
        }
        if (MedicalRecordId.HasValue &&
    existingVital.MedicalRecordId != MedicalRecordId.Value)
        {
            return BadRequest("Vital sign does not belong to the selected medical record.");
        }

        // Preserve authoritative fields from the database.
        VitalSignLog.MedicalRecordId = existingVital.MedicalRecordId;
        VitalSignLog.ClinicPatientId = existingVital.ClinicPatientId;
        VitalSignLog.StaffId = existingVital.StaffId;
        VitalSignLog.RecordedAt = existingVital.RecordedAt;

        // Update only editable vital-sign fields.
        existingVital.Temperature = VitalSignLog.Temperature;
        existingVital.HeartRate = VitalSignLog.HeartRate;
        existingVital.OxygenSaturation = VitalSignLog.OxygenSaturation;
        existingVital.SystolicBP = VitalSignLog.SystolicBP;
        existingVital.DiastolicBP = VitalSignLog.DiastolicBP;
        existingVital.VisitReason = VitalSignLog.VisitReason;

        // Re-evaluate the updated vital signs.
        VitalSignEvaluator.Evaluate(existingVital);

        // Reconcile existing active alerts for this vital record.
        var activeAlerts = await _context.Alerts
            .Where(a =>
                a.VitalLogId == existingVital.VitalSignLogId &&
                a.Status == "Active")
            .ToListAsync();

        foreach (var alert in activeAlerts)
        {
            alert.Status = "Resolved";
        }

        // Create a new alert if the updated reading is abnormal.
        if (existingVital.Status == "Warning" ||
            existingVital.Status == "Critical")
        {
            var firstRemark = existingVital.Remarks
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?.Trim()
                ?? "Abnormal Vital Signs";

            var alert = new Alert
            {
                VitalLogId = existingVital.VitalSignLogId,
                AlertType = firstRemark,
                AlertMessage = existingVital.Remarks,
                Status = "Active",
                CreatedAt = DateTime.Now
            };

            _context.Alerts.Add(alert);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!VitalSignLogExists(VitalSignLog.VitalSignLogId))
            {
                return NotFound();
            }

            throw;
        }

        if (MedicalRecordId.HasValue)
        {
            return RedirectToPage("./Index",
                new { medicalRecordId = MedicalRecordId });
        }

        return RedirectToPage("./Index");
    }
    private bool VitalSignLogExists(int id)
    {
        return _context.VitalSignLogs.Any(e => e.VitalSignLogId == id);
    }
}
