using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class ReassessmentHistoryModel : PageModel
{
    private readonly AppDbContext _context;

    public ReassessmentHistoryModel(AppDbContext context)
    {
        _context = context;
    }

    public VitalSignLog VitalSignLog { get; set; } = default!;

    public List<VitalSignReassessment> Reassessments { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var vital = await _context.VitalSignLogs
            .FirstOrDefaultAsync(v => v.VitalSignLogId == id);

        if (vital == null)
            return NotFound();

        VitalSignLog = vital;

        Reassessments = await _context.VitalSignReassessments
            .Where(r => r.VitalSignLogId == id)
            .OrderBy(r => r.ReassessmentNumber)
            .ToListAsync();

        return Page();
    }
}