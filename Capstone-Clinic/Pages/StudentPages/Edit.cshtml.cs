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
            return NotFound();

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student == null)
            return NotFound();

        Student = student;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // Load the existing student as a tracked entity.
        // This prevents accidentally overwriting database fields
        // that are not included in the Edit form.
        var existingStudent = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == Student.StudentId);

        if (existingStudent == null)
            return NotFound();

        // Update only the fields intentionally editable here.
        existingStudent.StudentNumber = Student.StudentNumber;
        existingStudent.FullName = Student.FullName;
        existingStudent.DateOfBirth = Student.DateOfBirth;
        existingStudent.Age = Student.Age;
        existingStudent.Gender = Student.Gender;
        existingStudent.Course = Student.Course;
        existingStudent.YearLevel = Student.YearLevel;
        existingStudent.MedicalRequirementStatus =
            Student.MedicalRequirementStatus;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StudentExists(Student.StudentId))
                return NotFound();

            throw;
        }

        return RedirectToPage(
            "/Student/Search",
            new
            {
                studentNumber = existingStudent.StudentNumber
            });
    }

    private bool StudentExists(int id)
    {
        return _context.Students.Any(s => s.StudentId == id);
    }
}