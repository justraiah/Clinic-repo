using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
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
        else
        {
            VitalSignLog = vitalsignlog;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
    int? id,
    int? medicalRecordId)
    {
        MedicalRecordId = medicalRecordId;

        if (id == null)
        {
            return NotFound();
        }

        var vitalsignlog = await _context.VitalSignLogs
            .FindAsync(id);

        if (vitalsignlog != null)
        {
            VitalSignLog = vitalsignlog;

            // Remove related reassessments first
            var relatedReassessments = await _context.VitalSignReassessments
                .Where(r => r.VitalSignLogId == vitalsignlog.VitalSignLogId)
                .ToListAsync();

            if (relatedReassessments.Count > 0)
            {
                _context.VitalSignReassessments
                    .RemoveRange(relatedReassessments);
            }

            // Remove related alerts
            var relatedAlerts = await _context.Alerts
                .Where(a => a.VitalLogId == vitalsignlog.VitalSignLogId)
                .ToListAsync();

            if (relatedAlerts.Count > 0)
            {
                _context.Alerts.RemoveRange(relatedAlerts);
            }

            // Remove the original vital-sign log last
            _context.VitalSignLogs.Remove(vitalsignlog);

            await _context.SaveChangesAsync();
        }

        if (MedicalRecordId.HasValue)
        {
            return RedirectToPage(
                "./Index",
                new { medicalRecordId = MedicalRecordId }
            );
        }

        return RedirectToPage("./Index");
    }
}
