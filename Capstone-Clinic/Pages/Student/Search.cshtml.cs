using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;

namespace Capstone_Clinic.Pages.Student;

public class SearchModel : PageModel
{
    private readonly AppDbContext _context;

    public SearchModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public string SearchStudentNumber { get; set; } = "";

    public Models.Student? Student { get; set; }

    public bool StudentNotFound { get; set; }

    public bool HasMedicalRecord { get; set; }

    public int MedicalRecordId { get; set; }

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostAsync()
    {
        Student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == SearchStudentNumber);

        if (Student == null)
        {
            StudentNotFound = true;
            return Page();
        }

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(m => m.StudentId == Student.StudentId);

        if (medicalRecord != null)
        {
            HasMedicalRecord = true;
            MedicalRecordId = medicalRecord.MedicalRecordId;
        }

        return Page();
    }
}