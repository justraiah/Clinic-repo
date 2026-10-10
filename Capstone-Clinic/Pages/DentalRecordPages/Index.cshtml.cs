
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.DentalRecordPages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Models.DentalRecord> DentalRecords { get; set; }
           = new List<Models.DentalRecord>();

        public int DentalRecordCount => DentalRecords.Count;
        public async Task OnGetAsync()
        {
            DentalRecords = await _context.DentalRecords
                .Include(d => d.ClinicPatient)
                    .ThenInclude(cp => cp!.Student)
                .OrderByDescending(d => d.VisitDate)
                .ToListAsync();
        }
    }
}
