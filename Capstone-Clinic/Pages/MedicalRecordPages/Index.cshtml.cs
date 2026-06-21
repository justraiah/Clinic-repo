using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<MedicalRecord> MedicalRecord { get; set; } = default!;

    public async Task OnGetAsync()
    {
        MedicalRecord = await _context.MedicalRecords.ToListAsync();
    }
}
