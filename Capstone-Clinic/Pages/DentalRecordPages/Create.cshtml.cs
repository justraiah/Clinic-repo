
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.DentalRecordPages
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DentalRecord DentalRecord { get; set; } = new();

        public SelectList PatientOptions { get; set; } = default!;



        private async Task LoadPatientsAsync()
        {
            var patients = await _context.ClinicPatients
                .AsNoTracking()
                .Include(p => p.Student)
                .ToListAsync();

            var patientOptions = patients.Select(p => new
            {
                ClinicPatientId = p.ClinicPatientId,
                DisplayName = p.Student != null
                    ? $"{p.Student.FullName} ({p.Student.StudentNumber})"
                    : $"{p.FullName ?? "Unnamed Patient"} ({p.PatientType})"
            }).ToList();

            PatientOptions = new SelectList(
                patientOptions,
                "ClinicPatientId",
                "DisplayName");
        }



        public async Task OnGetAsync()
        {
            DentalRecord.VisitDate = DateTime.Today;
            await LoadPatientsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!await _context.ClinicPatients
                    .AnyAsync(p => p.ClinicPatientId == DentalRecord.ClinicPatientId))
            {
                ModelState.AddModelError(
                    "DentalRecord.ClinicPatientId",
                    "Please select a valid patient.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPatientsAsync();
                return Page();
            }

            DentalRecord.CreatedAt = DateTime.Now;

            _context.DentalRecords.Add(DentalRecord);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
