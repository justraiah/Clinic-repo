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

    public async Task OnGetAsync()
    {
        VitalSigns = await (
            from v in _context.VitalSignLogs
            join m in _context.MedicalRecords
                on v.MedicalRecordId equals m.MedicalRecordId
            join s in _context.Students
                on m.StudentId equals s.StudentId
            select new VitalSignDisplayModel
            {
                VitalSignLogId = v.VitalSignLogId,
                Student = s.StudentNumber + " - " + s.FullName,
                Temperature = v.Temperature,
                HeartRate = v.HeartRate,
                OxygenSaturation = v.OxygenSaturation,
                SystolicBP = v.SystolicBP,
                DiastolicBP = v.DiastolicBP,
                RecordedAt = v.RecordedAt,
                StaffId = v.StaffId
            }).ToListAsync();
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
    }
}