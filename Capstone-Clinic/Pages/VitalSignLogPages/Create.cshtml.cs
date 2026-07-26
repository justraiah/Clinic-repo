using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class CreateModel : PageModel
{

    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        StudentList = new SelectList(
    _context.Students
        .Select(s => new
        {
            s.StudentId,
            Display = s.StudentNumber + " - " + s.FullName
        })
        .ToList(),
    "StudentId",
    "Display"
);

        VitalSignLog = new VitalSignLog
        {
            RecordedAt = DateTime.Now
        };

        return Page();
    }

    [BindProperty]
    public VitalSignLog VitalSignLog { get; set; } = default!;
    [BindProperty]
    public int SelectedStudentId { get; set; }
    public SelectList StudentList { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            StudentList = new SelectList(
                _context.Students
                    .Select(s => new
                    {
                        s.StudentId,
                        Display = s.StudentNumber + " - " + s.FullName
                    })
                    .ToList(),
                "StudentId",
                "Display"
            );

            return Page();
        }

        var medicalRecord = await _context.MedicalRecords
            .FirstOrDefaultAsync(m => m.StudentId == SelectedStudentId);

        if (medicalRecord == null)
        {
            ModelState.AddModelError("", "No medical record found for the selected student.");

            StudentList = new SelectList(
                _context.Students
                    .Select(s => new
                    {
                        s.StudentId,
                        Display = s.StudentNumber + " - " + s.FullName
                    })
                    .ToList(),
                "StudentId",
                "Display"
            );

            return Page();
        }

        VitalSignLog.MedicalRecordId = medicalRecord.MedicalRecordId;
        VitalSignLog.StaffId = 1;
        VitalSignLog.RecordedAt = DateTime.Now;

        _context.VitalSignLogs.Add(VitalSignLog);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
