using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.StudentPages;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Capstone_Clinic.Models.Student Student { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? studentid)
    {
        if (studentid is null)
        {
            return NotFound();
        }

        var student = await _context.Students.FirstOrDefaultAsync(m => m.StudentId == studentid);
        if (student is null)
        {
            return NotFound();
        }
        else
        {
            Student = student;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? studentid)
    {
        if (studentid is null)
        {
            return NotFound();
        }

        var student = await _context.Students.FindAsync(studentid);
        if (student != null)
        {
            Student = student;
            _context.Students.Remove(Student);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
