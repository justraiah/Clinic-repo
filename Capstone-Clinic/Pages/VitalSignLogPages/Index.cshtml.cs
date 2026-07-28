using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages.VitalSignLogPages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<VitalSignDisplayModel> VitalSigns { get; set; } = new();
    public int? MedicalRecordId { get; set; }

    public string StudentName { get; set; } = "";
    public int TotalVisits { get; set; }

    public int CriticalVisits { get; set; }

    public string LatestStatus { get; set; } = "";

    public DateTime? LastVisitDate { get; set; }
    public async Task OnGetAsync (int? medicalRecordId) 
    {
        MedicalRecordId = medicalRecordId;
        var query =
    from v in _context.VitalSignLogs
    join m in _context.MedicalRecords
        on v.MedicalRecordId equals m.MedicalRecordId
    join s in _context.Students
        on m.StudentId equals s.StudentId
    select new
    {
        Vital = v,
        Student = s
    };
        if (medicalRecordId.HasValue)
        {
            query = query.Where(x => x.Vital.MedicalRecordId == medicalRecordId.Value);

            var student = await _context.MedicalRecords
                .Where(m => m.MedicalRecordId == medicalRecordId.Value)
                .Join(_context.Students,
                      m => m.StudentId,
                      s => s.StudentId,
                      (m, s) => s.FullName)
                .FirstOrDefaultAsync();

            StudentName = student ?? "";
        }
        VitalSigns = await query
    .Select(x => new VitalSignDisplayModel
    {
        VitalSignLogId = x.Vital.VitalSignLogId,
        Student = x.Student.StudentNumber + " - " + x.Student.FullName,
        Temperature = x.Vital.Temperature,
        HeartRate = x.Vital.HeartRate,
        OxygenSaturation = x.Vital.OxygenSaturation,
        SystolicBP = x.Vital.SystolicBP,
        DiastolicBP = x.Vital.DiastolicBP,
        RecordedAt = x.Vital.RecordedAt,
        StaffId = x.Vital.StaffId,
        Status = x.Vital.Status,
        Remarks = x.Vital.Remarks,
        VisitReason = x.Vital.VisitReason
    })
    .ToListAsync();
        TotalVisits = VitalSigns.Count;

        CriticalVisits = VitalSigns.Count(v => v.Status == "Critical");

        if (VitalSigns.Any())
        {
            var latest = VitalSigns
                .OrderByDescending(v => v.RecordedAt)
                .First();

            LatestStatus = latest.Status;
            LastVisitDate = latest.RecordedAt;
        }
    }

    public class VitalSignDisplayModel
    {
        public int VitalSignLogId { get; set; }

        public string Student { get; set; } = "";

        public double Temperature { get; set; }

        public int HeartRate { get; set; }

        public int OxygenSaturation { get; set; }

        public int SystolicBP { get; set; }

        public int DiastolicBP { get; set; }

        public DateTime RecordedAt { get; set; }

        public int StaffId { get; set; }
        public string Status { get; set; } = "";

        public string Remarks { get; set; } = "";
        public string VisitReason { get; set; } = "";
    }
}