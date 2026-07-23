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
            _context.MedicalRecords.ToList(),
            "MedicalRecordId",
            "MedicalRecordId"
        );

        VitalSignLog = new VitalSignLog
        {
            RecordedAt = DateTime.Now
        };

        return Page();
    }

    [BindProperty]
    public VitalSignLog VitalSignLog { get; set; } = default!;
    public SelectList StudentList { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        VitalSignLog.StaffId = 1;
        VitalSignLog.RecordedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        _context.VitalSignLogs.Add(VitalSignLog);

        return RedirectToPage("./Index");
    }
}
