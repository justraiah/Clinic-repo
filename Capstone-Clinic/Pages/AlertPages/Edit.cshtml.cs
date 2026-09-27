using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.AlertPages;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
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
        Alert = alert;
        return Page();
    }

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
    public IActionResult OnPostAsync()
    {
        return Forbid();
    }

    private bool AlertExists(int id)
    {
        return _context.Alerts.Any(e => e.AlertId == id);
    }
}
