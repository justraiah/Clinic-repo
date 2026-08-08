using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync(int? studentId)
    {
        if (studentId == null)
        {
            return RedirectToPage("/Student/Search");
        }

        Student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (Student == null)
        {
            return NotFound();
        }

        StudentAge = DateTime.Today.Year - Student.DateOfBirth.Year;

        if (Student.DateOfBirth.Date > DateTime.Today.AddYears(-StudentAge))
        {
            StudentAge--;
        }

        MedicalRecord = new MedicalRecord
        {
            StudentId = Student.StudentId,
            VisitDate = DateTime.Today,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return Page();
    }

    public Capstone_Clinic.Models.Student? Student { get; set; }

    public int StudentAge { get; set; }
    [BindProperty]
    public MedicalRecord MedicalRecord { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        // Optional consultation fields should be stored as empty strings
        // because the existing database columns do not allow NULL.
        MedicalRecord.Medications ??= "";
        MedicalRecord.Allergies ??= "";
        MedicalRecord.ClinicalNotes ??= "";

        // Remove implicit validation errors for optional fields.
        ModelState.Remove("MedicalRecord.Medications");
        ModelState.Remove("MedicalRecord.Allergies");
        ModelState.Remove("MedicalRecord.ClinicalNotes");

        if (!ModelState.IsValid)
        {
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == MedicalRecord.StudentId);

            if (Student != null)
            {
                StudentAge = DateTime.Today.Year - Student.DateOfBirth.Year;
            }

            return Page();
        }

        MedicalRecord.CreatedAt = DateTime.Now;
        MedicalRecord.UpdatedAt = DateTime.Now;

        _context.MedicalRecords.Add(MedicalRecord);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "✅ Consultation saved successfully.";

        return RedirectToPage(
            "./Details",
            new { id = MedicalRecord.MedicalRecordId });
    }
}
