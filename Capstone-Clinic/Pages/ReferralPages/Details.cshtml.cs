
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.ReferralPages
{
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        public Referral Referral { get; set; } = default!;

        [BindProperty]
        public string SelectedStatus { get; set; } = "Pending";

        public static readonly string[] AvailableStatuses =
        {
            "Pending",
            "Referred",
            "Completed",
            "Cancelled"
        };

        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var referral = await _context.Referrals
                .AsNoTracking()
                .Include(r => r.ClinicPatient)
                    .ThenInclude(p => p!.Student)
                .FirstOrDefaultAsync(r => r.ReferralId == id);

            if (referral == null)
            {
                return NotFound();
            }

            Referral = referral;
            SelectedStatus = referral.Status;

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id)
        {
            var referral = await _context.Referrals
                .FirstOrDefaultAsync(r => r.ReferralId == id);

            if (referral == null)
            {
                return NotFound();
            }

            if (!AvailableStatuses.Contains(SelectedStatus))
            {
                ModelState.AddModelError(
                    nameof(SelectedStatus),
                    "Please select a valid referral status.");

                await LoadReferralAsync(id);
                return Page();
            }

            referral.Status = SelectedStatus;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", new { id });
        }

        private async Task LoadReferralAsync(int id)
        {
            Referral = await _context.Referrals
                .AsNoTracking()
                .Include(r => r.ClinicPatient)
                    .ThenInclude(p => p!.Student)
                .FirstAsync(r => r.ReferralId == id);
        }
    }
}
