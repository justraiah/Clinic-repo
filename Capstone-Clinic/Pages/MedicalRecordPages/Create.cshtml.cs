using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync(int? studentId, int? clinicPatientId)
    {
        IsExistingPatient = studentId.HasValue || clinicPatientId.HasValue;
        if (studentId == null && clinicPatientId.HasValue)
        {
            var clinicPatient = await _context.ClinicPatients
                .FirstOrDefaultAsync(cp =>
                    cp.ClinicPatientId == clinicPatientId.Value);

            if (clinicPatient == null)
            {
                return NotFound();
            }

            CommunityPatientType = clinicPatient.PatientType;
            CommunityFullName = clinicPatient.FullName;
            CommunityIdentifier = clinicPatient.Identifier;
            CommunityDateOfBirth = clinicPatient.DateOfBirth;

            MedicalRecord = new MedicalRecord
            {
                ClinicPatientId = clinicPatient.ClinicPatientId,
                VisitDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return Page();
        }

        if (studentId == null)
        {
            // Global/community consultation
            MedicalRecord = new MedicalRecord
            {
                VisitDate = DateTime.Today,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            return Page();
        }

        Student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId);

        if (Student == null)
        {
            return NotFound();
        }

        StudentAge = DateTime.Today.Year - Student.DateOfBirth.Year;

        if (Student.DateOfBirth.Date > DateTime.Today.AddYears(-StudentAge))
        {
            StudentAge--;
        }

        MedicalRecord = new MedicalRecord
        {
            StudentId = Student.StudentId,
            VisitDate = DateTime.Today,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return Page();
    }

    public Capstone_Clinic.Models.Student? Student { get; set; }

    public int StudentAge { get; set; }
    [BindProperty]
    public string? CommunityPatientType { get; set; }

    [BindProperty]
    public string? CommunityFullName { get; set; }

    [BindProperty]
    public string? CommunityIdentifier { get; set; }

    [BindProperty]
    public DateTime? CommunityDateOfBirth { get; set; }

    [BindProperty]
    public MedicalRecord MedicalRecord { get; set; } = default!;
    public bool IsExistingPatient { get; set; }

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        // Optional consultation fields should be stored as empty strings
        // because the existing database columns do not allow NULL.
        MedicalRecord.Medications ??= "";
        MedicalRecord.Allergies ??= "";
        MedicalRecord.ClinicalNotes ??= "";

        // Remove implicit validation errors for optional fields.
        ModelState.Remove("MedicalRecord.Medications");
        ModelState.Remove("MedicalRecord.Allergies");
        ModelState.Remove("MedicalRecord.ClinicalNotes");

        // Student consultation
        if (MedicalRecord.StudentId.HasValue)
        {
            var clinicPatient = await _context.ClinicPatients
                .FirstOrDefaultAsync(cp => cp.StudentId == MedicalRecord.StudentId);

            if (clinicPatient == null)
            {
                ModelState.AddModelError(
                    "",
                    "No clinic patient record exists for this student."
                );
            }
            else
            {
                MedicalRecord.ClinicPatientId = clinicPatient.ClinicPatientId;
            }
        }
        // Community consultation
        else
        {
            // Existing community patient
            if (MedicalRecord.ClinicPatientId > 0)
            {
                var clinicPatient = await _context.ClinicPatients
                    .FirstOrDefaultAsync(cp =>
                        cp.ClinicPatientId == MedicalRecord.ClinicPatientId);

                if (clinicPatient == null)
                {
                    ModelState.AddModelError(
                        "",
                        "The selected community patient could not be found."
                    );
                }
            }
            // New community patient
            else
            {
                if (string.IsNullOrWhiteSpace(CommunityFullName))
                {
                    ModelState.AddModelError(
                        nameof(CommunityFullName),
                        "Full name is required."
                    );
                }

                if (string.IsNullOrWhiteSpace(CommunityPatientType))
                {
                    ModelState.AddModelError(
                        nameof(CommunityPatientType),
                        "Patient type is required."
                    );
                }

                if (ModelState.IsValid)
                {
                    var clinicPatient = new ClinicPatient
                    {
                        PatientType = CommunityPatientType!,
                        FullName = CommunityFullName,
                        Identifier = CommunityIdentifier,
                        DateOfBirth = CommunityDateOfBirth
                    };

                    _context.ClinicPatients.Add(clinicPatient);
                    await _context.SaveChangesAsync();

                    MedicalRecord.ClinicPatientId = clinicPatient.ClinicPatientId;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            if (MedicalRecord.StudentId.HasValue)
            {
                Student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == MedicalRecord.StudentId);

                if (Student != null)
                {
                    StudentAge = DateTime.Today.Year - Student.DateOfBirth.Year;

                    if (Student.DateOfBirth.Date > DateTime.Today.AddYears(-StudentAge))
                    {
                        StudentAge--;
                    }
                }
            }

            return Page();
        }

        MedicalRecord.CreatedAt = DateTime.Now;
        MedicalRecord.UpdatedAt = DateTime.Now;

        _context.MedicalRecords.Add(MedicalRecord);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "✅ Consultation saved successfully.";

        return RedirectToPage(
            "./Details",
            new { id = MedicalRecord.MedicalRecordId });
    }
}