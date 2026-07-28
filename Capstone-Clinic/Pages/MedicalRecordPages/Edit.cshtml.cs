using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public MedicalRecord MedicalRecord { get; set; } = default!;
    public string StudentNumber { get; set; } = "";
    public string StudentName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var medicalrecord = await _context.MedicalRecords.FirstOrDefaultAsync(m => m.MedicalRecordId == id);
        if (medicalrecord is null)
        {
            return NotFound();
        }
        MedicalRecord = medicalrecord;

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == medicalrecord.StudentId);

        if (student != null)
        {
            StudentNumber = student.StudentNumber;
            StudentName = student.FullName;
        }

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

        _context.Attach(MedicalRecord).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MedicalRecordExists(MedicalRecord.MedicalRecordId))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool MedicalRecordExists(int id)
    {
        return _context.MedicalRecords.Any(e => e.MedicalRecordId == id);
    }
}
