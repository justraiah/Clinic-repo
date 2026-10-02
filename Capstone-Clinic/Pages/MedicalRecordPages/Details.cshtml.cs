using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public MedicalRecord MedicalRecord { get; set; } = default!;

    public List<MedicalRecord> ConsultationHistory { get; set; } = new();

    public int TotalVisits { get; set; }

    public int CriticalVisits { get; set; }

    public string LatestStatus { get; set; } = "";

    public Capstone_Clinic.Models.Student? Student { get; set; }

    public int StudentAge { get; set; }

    public List<VitalSignLog> RecentVitalSigns { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var medicalRecord = await _context.MedicalRecords
            .Include(m => m.ClinicPatient)
            .FirstOrDefaultAsync(m => m.MedicalRecordId == id);

        if (medicalRecord == null)
        {
            return NotFound();
        }

        MedicalRecord = medicalRecord;

        // Student consultation
        if (MedicalRecord.StudentId.HasValue)
        {
            Student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == MedicalRecord.StudentId);

            if (Student != null && Student.DateOfBirth != default)
            {
                StudentAge = DateTime.Today.Year - Student.DateOfBirth.Year;

                if (Student.DateOfBirth.Date > DateTime.Today.AddYears(-StudentAge))
                {
                    StudentAge--;
                }
            }

            // Get every consultation for this student
            ConsultationHistory = await _context.MedicalRecords
                .Where(m => m.StudentId == MedicalRecord.StudentId)
                .OrderByDescending(m => m.VisitDate)
                .ToListAsync();
        }
        // Community consultation
        else
        {
            // Get every consultation for this community patient
            ConsultationHistory = await _context.MedicalRecords
                .Where(m => m.ClinicPatientId == MedicalRecord.ClinicPatientId)
                .OrderByDescending(m => m.VisitDate)
                .ToListAsync();
        }

        // Get all medical record IDs belonging to this patient
        var medicalRecordIds = ConsultationHistory
            .Select(m => m.MedicalRecordId)
            .ToList();

        // Patient-wide recent vital signs
        RecentVitalSigns = await _context.VitalSignLogs
            .Where(v => medicalRecordIds.Contains(v.MedicalRecordId))
            .OrderByDescending(v => v.RecordedAt)
            .Take(5)
            .ToListAsync();

        // Patient-wide statistics
        TotalVisits = await _context.VitalSignLogs
            .CountAsync(v => medicalRecordIds.Contains(v.MedicalRecordId));

        CriticalVisits = await _context.VitalSignLogs
            .CountAsync(v =>
                medicalRecordIds.Contains(v.MedicalRecordId) &&
                v.Status == "Critical");

        LatestStatus = await _context.VitalSignLogs
            .Where(v => medicalRecordIds.Contains(v.MedicalRecordId))
            .OrderByDescending(v => v.RecordedAt)
            .Select(v => v.Status)
            .FirstOrDefaultAsync() ?? "No Records";

        return Page();
    }
}