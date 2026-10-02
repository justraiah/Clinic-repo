using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.AlertPages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<AlertViewModel> Alerts { get; set; } = new();
    public int CriticalCount { get; set; }

    public int WarningCount { get; set; }

    public int AcknowledgedCount { get; set; }

    public async Task OnGetAsync()
    {

        Alerts = await (
    from alert in _context.Alerts
    join vital in _context.VitalSignLogs
        on alert.VitalLogId equals vital.VitalSignLogId
    join medical in _context.MedicalRecords
        on vital.MedicalRecordId equals medical.MedicalRecordId
    join clinicPatient in _context.ClinicPatients
        on vital.ClinicPatientId equals clinicPatient.ClinicPatientId
    join student in _context.Students
        on medical.StudentId equals student.StudentId into studentGroup
    from student in studentGroup.DefaultIfEmpty()

    orderby alert.CreatedAt descending

    select new AlertViewModel
    {
        AlertId = alert.AlertId,

        StudentNumber = student != null
            ? student.StudentNumber
            : clinicPatient.Identifier ?? "",

        StudentName = student != null
            ? student.FullName
            : clinicPatient.FullName ?? "Community Member",

        AlertType = alert.AlertType,
        Severity = vital.Status,
        Status = alert.Status,
        CreatedAt = alert.CreatedAt
    }

).ToListAsync();
        CriticalCount = await _context.Alerts
    .Join(_context.VitalSignLogs,
        a => a.VitalLogId,
        v => v.VitalSignLogId,
        (a, v) => new { Alert = a, Vital = v })
    .CountAsync(x =>
        x.Alert.Status == "Active" &&
        x.Vital.Status == "Critical");

        WarningCount = await _context.Alerts
            .Join(_context.VitalSignLogs,
                a => a.VitalLogId,
                v => v.VitalSignLogId,
                (a, v) => new { Alert = a, Vital = v })
            .CountAsync(x =>
                x.Alert.Status == "Active" &&
                x.Vital.Status == "Warning");

        AcknowledgedCount = await _context.Alerts
            .CountAsync(a => a.Status == "Acknowledged");
    }
    public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
    {
        var alert = await _context.Alerts.FindAsync(id);

        if (alert == null)
        {
            return NotFound();
        }

        alert.Status = "Acknowledged";

        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
    public class AlertViewModel
    {
        public int AlertId { get; set; }

        public string StudentNumber { get; set; } = "";

        public string StudentName { get; set; } = "";

        public string AlertType { get; set; } = "";

        public string Severity { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}