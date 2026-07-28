using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.AlertPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Alert Alert { get; set; } = default!;
    public VitalSignLog VitalSign { get; set; } = default!;

    public MedicalRecord MedicalRecord { get; set; } = default!;

    public Capstone_Clinic.Models.Student Student { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var alert = await _context.Alerts.FirstOrDefaultAsync(m => m.AlertId == id);
        if (alert is null)
        {
            return NotFound();
        }
        else
        {
            Alert = alert;

            VitalSign = await _context.VitalSignLogs
                .FirstOrDefaultAsync(v => v.VitalSignLogId == Alert.VitalLogId)
                ?? new VitalSignLog();

            MedicalRecord = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.MedicalRecordId == VitalSign.MedicalRecordId)
                ?? new MedicalRecord();

            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == MedicalRecord.StudentId)
                ?? new Capstone_Clinic.Models.Student();
        }

        return Page();
    }
}
