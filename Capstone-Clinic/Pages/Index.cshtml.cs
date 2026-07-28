using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Capstone_Clinic.Data;

namespace Capstone_Clinic.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public int StudentCount { get; set; }

        public int MedicalRecordCount { get; set; }

        public int VitalSignCount { get; set; }

        public int ActiveAlertCount { get; set; }
        public string DatabaseStatus { get; set; } = "Connected";

        public string AlertEngineStatus { get; set; } = "Running";

        public string IoTDeviceStatus { get; set; } = "Offline";

        public DateTime LastUpdated { get; set; }
        public List<ActivityViewModel> RecentActivities { get; set; } = new();

        public async Task OnGetAsync()
        {
            LastUpdated = DateTime.Now;
            StudentCount = await _context.Students.CountAsync();

            MedicalRecordCount = await _context.MedicalRecords.CountAsync();

            VitalSignCount = await _context.VitalSignLogs.CountAsync();

            ActiveAlertCount = await _context.Alerts
                .CountAsync(a => a.Status == "Active");
            RecentActivities = await (
    from alert in _context.Alerts
    join vital in _context.VitalSignLogs
        on alert.VitalLogId equals vital.VitalSignLogId
    join medical in _context.MedicalRecords
        on vital.MedicalRecordId equals medical.MedicalRecordId
    join student in _context.Students
        on medical.StudentId equals student.StudentId

    orderby alert.CreatedAt descending

    select new ActivityViewModel
    {
        StudentName = student.FullName,
        StudentNumber = student.StudentNumber,
        Activity = alert.AlertType,
        Severity = alert.Status,
        Time = alert.CreatedAt
    }

).Take(5).ToListAsync();
        }
    }
    public class ActivityViewModel
    {
        public string StudentNumber { get; set; } = "";

        public string StudentName { get; set; } = "";

        public string Activity { get; set; } = "";

        public string Severity { get; set; } = "";

        public DateTime Time { get; set; }
    }
}