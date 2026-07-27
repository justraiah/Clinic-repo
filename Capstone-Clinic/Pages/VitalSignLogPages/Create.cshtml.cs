using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Capstone_Clinic.Helpers;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class CreateModel : PageModel
{

    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    public string StudentNumber { get; set; } = "";

    public string StudentName { get; set; } = "";

    public IActionResult OnGet(int? medicalRecordId)
    {
        if (medicalRecordId.HasValue)
        {
            VitalSignLog = new VitalSignLog
            {
                MedicalRecordId = medicalRecordId.Value,
                RecordedAt = DateTime.Now
            };

            var medicalRecord = _context.MedicalRecords
                .FirstOrDefault(m => m.MedicalRecordId == medicalRecordId.Value);

            if (medicalRecord != null)
            {
                var student = _context.Students
                    .FirstOrDefault(s => s.StudentId == medicalRecord.StudentId);

                if (student != null)
                {
                    StudentNumber = student.StudentNumber;
                    StudentName = student.FullName;
                }
            }

            return Page();
        }

        // Fallback if opened directly
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

        if (VitalSignLog.MedicalRecordId == 0)
        {
            ModelState.AddModelError("", "Medical Record not found.");

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
        VitalSignLog.StaffId = 1;
        VitalSignLog.RecordedAt = DateTime.Now;
        VitalSignEvaluator.Evaluate(VitalSignLog);
        _context.VitalSignLogs.Add(VitalSignLog);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
