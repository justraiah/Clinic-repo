
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.DentalRecordPages
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
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


        public async Task<IActionResult> OnGetAsync(int id)
        {
            var record = await _context.DentalRecords
                .AsNoTracking()
                .Include(d => d.ClinicPatient)
                    .ThenInclude(cp => cp!.Student)
                .FirstOrDefaultAsync(d => d.DentalRecordId == id);

            if (record is null)
            {
                return NotFound();
            }

            DentalRecord = record;
            await LoadPatientsAsync();

            return Page();
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

            var record = await _context.DentalRecords
                .FirstOrDefaultAsync(d =>
                    d.DentalRecordId == DentalRecord.DentalRecordId);

            if (record is null)
            {
                return NotFound();
            }

            record.VisitDate = DentalRecord.VisitDate;
            record.ChiefComplaint = DentalRecord.ChiefComplaint;
            record.DentalFindings = DentalRecord.DentalFindings;
            record.Diagnosis = DentalRecord.Diagnosis;
            record.TreatmentProvided = DentalRecord.TreatmentProvided;
            record.Medications = DentalRecord.Medications;
            record.ClinicalNotes = DentalRecord.ClinicalNotes;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
