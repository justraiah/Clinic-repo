using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Data;

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

    public async Task OnGetAsync(string? studentNumber)
    {
        if (!string.IsNullOrWhiteSpace(studentNumber))
        {
            SearchStudentNumber = studentNumber;

            await LoadStudentAsync();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadStudentAsync();

        return Page();
    }

    private async Task LoadStudentAsync()
    {
        Student = await _context.Students
    .FirstOrDefaultAsync(s =>

        s.StudentNumber == SearchStudentNumber ||

        s.FullName.ToLower()
            .Contains(SearchStudentNumber.ToLower())

    );

        if (Student == null)
        {
            StudentNotFound = true;
            return;
        }

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(
                m => m.StudentId == Student.StudentId
            );

        if (medicalRecord != null)
        {
            HasMedicalRecord = true;
            MedicalRecordId = medicalRecord.MedicalRecordId;
        }
    }
}