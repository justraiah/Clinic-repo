
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.ReferralPages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Referral> Referrals { get; set; } = new List<Referral>();

        public async Task OnGetAsync()
        {
            Referrals = await _context.Referrals
                .AsNoTracking()
                .Include(r => r.ClinicPatient)
                    .ThenInclude(p => p!.Student)
                .OrderByDescending(r => r.ReferralDate)
                .ToListAsync();
        }
    }
}
