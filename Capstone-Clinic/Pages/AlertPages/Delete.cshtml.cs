using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.AlertPages;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Alert Alert { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var alert = await _context.Alerts.FirstOrDefaultAsync(m => m.AlertId == id);
        if (alert is null)
        {
            return NotFound();
        }
        else
        {
            Alert = alert;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var alert = await _context.Alerts.FindAsync(id);
        if (alert != null)
        {
            Alert = alert;
            _context.Alerts.Remove(Alert);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
