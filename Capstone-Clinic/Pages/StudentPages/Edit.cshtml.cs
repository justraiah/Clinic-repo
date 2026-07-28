using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.StudentPages;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Capstone_Clinic.Models.Student Student { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var student = await _context.Students.FirstOrDefaultAsync(m => m.StudentId == id);

        if (student == null)
        {
            return NotFound();
        }

        Student = student;
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

        _context.Attach(Student).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StudentExists(Student.StudentId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage(
    "/Student/Search",
    new
    {
        studentNumber = Student.StudentNumber
    });
    }

    private bool StudentExists(int id)
    {
        return _context.Students.Any(e => e.StudentId == id);
    }
}