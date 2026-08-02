using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public int TotalStudents { get; set; }

    public int TotalMedicalRecords { get; set; }

    public int TotalVitalSigns { get; set; }

    public int ActiveAlerts { get; set; }
    public int NormalCount { get; set; }

    public int WarningCount { get; set; }

    public int CriticalCount { get; set; }
    public double NormalPercentage { get; set; }

    public double WarningPercentage { get; set; }

    public double CriticalPercentage { get; set; }
    public List<VisitReasonSummary> TopVisitReasons { get; set; } = new();
    public List<TopCriticalStudentViewModel> TopCriticalStudents { get; set; } = new();
    public List<RecentCriticalAlertViewModel> RecentCriticalAlerts { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "All";

    public async Task OnGetAsync()
    {
        DateTime? startDate = null;

        switch (Period)
        {
            case "Today":
                startDate = DateTime.Today;
                break;

            case "Week":
                startDate = DateTime.Today.AddDays(-7);
                break;

            case "Month":
                startDate = DateTime.Today.AddMonths(-1);
                break;

            case "Year":
                startDate = DateTime.Today.AddYears(-1);
                break;

            case "All":
            default:
                startDate = null;
                break;
        }
        var vitalQuery = _context.VitalSignLogs.AsQueryable();

        if (startDate.HasValue)
        {
            vitalQuery = vitalQuery.Where(v => v.RecordedAt >= startDate.Value);
        }
        TotalStudents = await _context.Students.CountAsync();

        TotalMedicalRecords = await _context.MedicalRecords.CountAsync();

        TotalVitalSigns = await vitalQuery.CountAsync();

        NormalCount = await vitalQuery
            .CountAsync(v => v.Status == "Normal");

        WarningCount = await vitalQuery
            .CountAsync(v => v.Status == "Warning");

        CriticalCount = await vitalQuery
            .CountAsync(v => v.Status == "Critical");
        TopVisitReasons = await vitalQuery
    .Where(v => !string.IsNullOrWhiteSpace(v.VisitReason))
    .GroupBy(v => v.VisitReason)
    .Select(g => new VisitReasonSummary
    {
        VisitReason = g.Key!,
        Count = g.Count()
    })
    .OrderByDescending(x => x.Count)
    .Take(5)
    .ToListAsync();
        TopCriticalStudents = await (
    from vital in vitalQuery
    join medical in _context.MedicalRecords
        on vital.MedicalRecordId equals medical.MedicalRecordId
    join student in _context.Students
        on medical.StudentId equals student.StudentId

    where vital.Status == "Critical"

    group student by new
    {
        student.StudentNumber,
        student.FullName
    }
    into g

    orderby g.Count() descending

    select new TopCriticalStudentViewModel
    {
        StudentNumber = g.Key.StudentNumber,
        StudentName = g.Key.FullName,
        CriticalVisits = g.Count()
    }

).ToListAsync();
        if (TotalVitalSigns > 0)
        {
            NormalPercentage = (double)NormalCount / TotalVitalSigns * 100;

            WarningPercentage = (double)WarningCount / TotalVitalSigns * 100;

            CriticalPercentage = (double)CriticalCount / TotalVitalSigns * 100;
        }
        RecentCriticalAlerts = await (
    from alert in _context.Alerts
    join vital in vitalQuery
        on alert.VitalLogId equals vital.VitalSignLogId
    join medical in _context.MedicalRecords
        on vital.MedicalRecordId equals medical.MedicalRecordId
    join student in _context.Students
        on medical.StudentId equals student.StudentId

    where vital.Status == "Critical"

    orderby alert.CreatedAt descending

    select new RecentCriticalAlertViewModel
    {
        StudentName = student.FullName,
        AlertType = alert.AlertType,
        CreatedAt = alert.CreatedAt
    }

).Take(5).ToListAsync();
    }

}
public class VisitReasonSummary
{
    public string VisitReason { get; set; } = "";

    public int Count { get; set; }
}
public class TopCriticalStudentViewModel
{
    public string StudentNumber { get; set; } = "";

    public string StudentName { get; set; } = "";

    public int CriticalVisits { get; set; }
}
public class RecentCriticalAlertViewModel
{
    public string StudentName { get; set; } = "";

    public string AlertType { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}