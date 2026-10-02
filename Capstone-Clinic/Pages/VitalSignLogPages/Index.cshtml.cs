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
    public List<StudentVitalSummary> StudentSummaries { get; set; } = new();
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
    join cp in _context.ClinicPatients
        on v.ClinicPatientId equals cp.ClinicPatientId
    join s in _context.Students
        on m.StudentId equals s.StudentId into studentGroup
    from s in studentGroup.DefaultIfEmpty()
    select new
    {
        Vital = v,
        MedicalRecord = m,
        ClinicPatient = cp,
        Student = s
    };
        if (medicalRecordId.HasValue)
        {
            var medicalRecord = await _context.MedicalRecords
                .Where(m => m.MedicalRecordId == medicalRecordId.Value)
                .Select(m => new
                {
                    m.StudentId,
                    m.ClinicPatientId
                })
                .FirstOrDefaultAsync();

            if (medicalRecord == null)
            {
                VitalSigns = new List<VitalSignDisplayModel>();
                return;
            }

            if (medicalRecord.StudentId.HasValue)
            {
                // Student: show vital signs from all consultations belonging to that student
                StudentName = await _context.Students
                    .Where(s => s.StudentId == medicalRecord.StudentId.Value)
                    .Select(s => s.FullName)
                    .FirstOrDefaultAsync() ?? "";

                var medicalRecordIds = await _context.MedicalRecords
                    .Where(m => m.StudentId == medicalRecord.StudentId.Value)
                    .Select(m => m.MedicalRecordId)
                    .ToListAsync();

                query = query.Where(x =>
                    medicalRecordIds.Contains(x.Vital.MedicalRecordId));
            }
            else
            {
                // Community patient: show vital signs from this clinic patient
                var communityPatient = await _context.ClinicPatients
                    .Where(cp => cp.ClinicPatientId == medicalRecord.ClinicPatientId)
                    .Select(cp => cp.FullName)
                    .FirstOrDefaultAsync();

                StudentName = communityPatient ?? "Community Patient";

                query = query.Where(x =>
                    x.Vital.ClinicPatientId == medicalRecord.ClinicPatientId);
            }
        }
        if (!medicalRecordId.HasValue)
        {
            StudentSummaries = await
(
    from m in _context.MedicalRecords
    join s in _context.Students
        on m.StudentId equals s.StudentId
    select new StudentVitalSummary
    {
        StudentId = s.StudentId,
        MedicalRecordId = m.MedicalRecordId,

        StudentName = s.FullName,
        StudentNumber = s.StudentNumber,

        LastRecorded = _context.VitalSignLogs
        .Where(v => v.MedicalRecordId == m.MedicalRecordId)
        .OrderByDescending(v => v.RecordedAt)
        .Select(v => v.RecordedAt)
        .FirstOrDefault(),

        LatestStatus = _context.VitalSignLogs
        .Where(v => v.MedicalRecordId == m.MedicalRecordId)
        .OrderByDescending(v => v.RecordedAt)
        .Select(v => v.Status)
        .FirstOrDefault() ?? "No Records",

        TotalRecords = _context.VitalSignLogs
        .Count(v => v.MedicalRecordId == m.MedicalRecordId),

        CriticalCount = _context.VitalSignLogs
        .Count(v =>
            v.MedicalRecordId == m.MedicalRecordId &&
            v.Status == "Critical")
    }
).ToListAsync();
        }
        VitalSigns = await query
    .OrderByDescending(x => x.Vital.RecordedAt)
    .Select(x => new VitalSignDisplayModel
    {
        VitalSignLogId = x.Vital.VitalSignLogId,
        Student = x.Student != null
    ? x.Student.StudentNumber + " - " + x.Student.FullName
    : x.ClinicPatient.FullName + " - " + x.ClinicPatient.Identifier,
        Temperature = x.Vital.Temperature,
        HeartRate = x.Vital.HeartRate,
        RespiratoryRate = x.Vital.RespiratoryRate,
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
    public class StudentVitalSummary
    {
        public int StudentId { get; set; }

        public int MedicalRecordId { get; set; }

        public string StudentName { get; set; } = "";
        public string StudentNumber { get; set; } = "";

        public DateTime LastRecorded { get; set; }

        public string LatestStatus { get; set; } = "";

        public int TotalRecords { get; set; }

        public int CriticalCount { get; set; }
    }
    public class VitalSignDisplayModel
    {
        public int VitalSignLogId { get; set; }

        public string Student { get; set; } = "";

        public double Temperature { get; set; }

        public int HeartRate { get; set; }
        public int RespiratoryRate { get; set; }

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