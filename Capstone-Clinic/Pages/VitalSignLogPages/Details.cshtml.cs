using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public VitalSignLog VitalSignLog { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
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
}
