
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.ReferralPages
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Referral Referral { get; set; } = new();

        public SelectList PatientOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            Referral.ReferralDate = DateTime.Today;
            await LoadPatientsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var patientExists = await _context.ClinicPatients
                .AnyAsync(p => p.ClinicPatientId == Referral.ClinicPatientId);

            if (!patientExists)
            {
                ModelState.AddModelError(
                    "Referral.ClinicPatientId",
                    "Please select a valid patient.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPatientsAsync();
                return Page();
            }

            Referral.CreatedAt = DateTime.Now;
            Referral.Status = "Pending";

            _context.Referrals.Add(Referral);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadPatientsAsync()
        {
            var patients = await _context.ClinicPatients
                .AsNoTracking()
                .Include(p => p.Student)
                .OrderBy(p => p.ClinicPatientId)
                .ToListAsync();

            var patientOptions = patients.Select(p => new
            {
                p.ClinicPatientId,
                DisplayName = p.Student != null
                    ? $"{p.Student.FullName} ({p.Student.StudentNumber})"
                    : $"{p.FullName ?? "Unnamed Patient"} ({p.PatientType})"
            }).ToList();

            PatientOptions = new SelectList(
                patientOptions,
                "ClinicPatientId",
                "DisplayName");
        }
    }
}
