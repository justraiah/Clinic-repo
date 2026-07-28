using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public VitalSignLog VitalSignLog { get; set; } = default!;
    public int? MedicalRecordId { get; set; }
    public string StudentName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(int? id, int? medicalRecordId)
    {
        MedicalRecordId = medicalRecordId;
        if (id == null)
        {
            return NotFound();
        }

        var vitalsignlog = await _context.VitalSignLogs.FirstOrDefaultAsync(m => m.VitalSignLogId == id);
        if (vitalsignlog is null)
        {
            return NotFound();
        }
        else
        {
            VitalSignLog = vitalsignlog;
            StudentName = await _context.MedicalRecords
    .Where(m => m.MedicalRecordId == VitalSignLog.MedicalRecordId)
    .Join(_context.Students,
        m => m.StudentId,
        s => s.StudentId,
        (m, s) => s.StudentNumber + " - " + s.FullName)
    .FirstOrDefaultAsync() ?? "";
        }

        return Page();
    }
}
