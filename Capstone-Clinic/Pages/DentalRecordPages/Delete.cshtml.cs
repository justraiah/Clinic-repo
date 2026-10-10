
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.DentalRecordPages
{
    public class DeleteModel : PageModel
    {
        private readonly AppDbContext _context;

        public DeleteModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DentalRecord? DentalRecord { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            DentalRecord = await _context.DentalRecords
                .Include(d => d.ClinicPatient)
                .FirstOrDefaultAsync(d => d.DentalRecordId == id);

            if (DentalRecord is null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var record = await _context.DentalRecords
                .FirstOrDefaultAsync(d =>
                    d.DentalRecordId == DentalRecord!.DentalRecordId);

            if (record is null)
            {
                return NotFound();
            }

            _context.DentalRecords.Remove(record);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
