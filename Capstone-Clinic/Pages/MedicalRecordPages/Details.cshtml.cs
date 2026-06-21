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
        else
        {
            MedicalRecord = medicalrecord;
        }

        return Page();
    }
}
