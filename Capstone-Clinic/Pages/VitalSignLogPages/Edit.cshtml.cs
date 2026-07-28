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
        VitalSignEvaluator.Evaluate(VitalSignLog);

        _context.Attach(VitalSignLog).State = EntityState.Modified;

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
            else
            {
                throw;
            }
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
