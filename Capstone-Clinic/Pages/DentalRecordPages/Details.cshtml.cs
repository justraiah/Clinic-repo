
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.DentalRecordPages
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public DentalRecord? DentalRecord { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            DentalRecord = await _context.DentalRecords
            .Include(d => d.ClinicPatient)
              .ThenInclude(cp => cp!.Student)
            .FirstOrDefaultAsync(d => d.DentalRecordId == id);

            if (DentalRecord is null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
