using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Models;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.MedicalRecordPages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<MedicalRecord> MedicalRecord { get; set; } = default!;

    public Dictionary<int, string> StudentDisplay { get; set; } = new();
    public Dictionary<int, string> CommunityPatientDisplay { get; set; } = new();

    public async Task OnGetAsync()
    {
        MedicalRecord = await _context.MedicalRecords
            .ToListAsync();

        var studentIds = MedicalRecord
    .Where(m => m.StudentId.HasValue)
    .Select(m => m.StudentId!.Value)
    .Distinct()
    .ToList();

        StudentDisplay = await _context.Students
            .Where(s => studentIds.Contains(s.StudentId))
            .ToDictionaryAsync(
                s => s.StudentId,
                s => s.StudentNumber + " - " + s.FullName
            );
        var communityPatientIds = MedicalRecord
    .Where(m => !m.StudentId.HasValue)
    .Select(m => m.ClinicPatientId)
    .Distinct()
    .ToList();

        CommunityPatientDisplay = await _context.ClinicPatients
            .Where(cp => communityPatientIds.Contains(cp.ClinicPatientId))
            .ToDictionaryAsync(
                cp => cp.ClinicPatientId,
                cp => !string.IsNullOrWhiteSpace(cp.Identifier)
                    ? cp.FullName + " - " + cp.Identifier
                    : cp.FullName ?? "Community Member"
            );
    }
}