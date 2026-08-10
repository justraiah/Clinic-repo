using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.StudentPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Capstone_Clinic.Models.Student Student { get; set; } = default!;

    public StudentAccount? StudentAccount { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(m => m.StudentId == id);

        if (student is null)
        {
            return NotFound();
        }

        Student = student;

        StudentAccount = await _context.StudentAccounts
            .FirstOrDefaultAsync(a => a.StudentId == id);

        return Page();
    }
}