using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public MedicalRecord MedicalRecord { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var medicalrecord = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.MedicalRecordId == id);
        if (medicalrecord is null)
        {
            return NotFound();
        }
        else
        {
            MedicalRecord = medicalrecord;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var medicalrecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

        if (medicalrecord != null)
        {
            MedicalRecord = medicalrecord;

            var vitalSignLogs = await _context.VitalSignLogs
                .Where(v => v.MedicalRecordId == id)
                .ToListAsync();

            var vitalSignLogIds = vitalSignLogs
                .Select(v => v.VitalSignLogId)
                .ToList();

            var alerts = await _context.Alerts
                .Where(a => vitalSignLogIds.Contains(a.VitalLogId))
                .ToListAsync();

            _context.Alerts.RemoveRange(alerts);
            _context.VitalSignLogs.RemoveRange(vitalSignLogs);
            _context.MedicalRecords.Remove(MedicalRecord);

            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
