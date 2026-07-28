using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public MedicalRecord MedicalRecord { get; set; } = default!;
    public int TotalVisits { get; set; }

    public int CriticalVisits { get; set; }

    public string LatestStatus { get; set; } = "";

    public Capstone_Clinic.Models.Student Student { get; set; } = default!;
    public List<VitalSignLog> RecentVitalSigns { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var medicalrecord = await _context.MedicalRecords
    .FirstOrDefaultAsync(m => m.MedicalRecordId == id);
        if (medicalrecord is null)
        {
            return NotFound();
        }
        else
        {
            MedicalRecord = medicalrecord;

            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == MedicalRecord.StudentId)
                ?? new Capstone_Clinic.Models.Student();

            RecentVitalSigns = await _context.VitalSignLogs
    .Where(v => v.MedicalRecordId == MedicalRecord.MedicalRecordId)
    .OrderByDescending(v => v.RecordedAt)
    .Take(5)
    .ToListAsync();
            TotalVisits = await _context.VitalSignLogs
    .CountAsync(v => v.MedicalRecordId == MedicalRecord.MedicalRecordId);

            CriticalVisits = await _context.VitalSignLogs
                .CountAsync(v =>
                    v.MedicalRecordId == MedicalRecord.MedicalRecordId &&
                    v.Status == "Critical");

            LatestStatus = await _context.VitalSignLogs
                .Where(v => v.MedicalRecordId == MedicalRecord.MedicalRecordId)
                .OrderByDescending(v => v.RecordedAt)
                .Select(v => v.Status)
                .FirstOrDefaultAsync() ?? "No Records";
        }

        return Page();
    }
}

