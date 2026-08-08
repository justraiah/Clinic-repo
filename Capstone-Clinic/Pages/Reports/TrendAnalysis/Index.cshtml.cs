using Capstone_Clinic.Data;
using Capstone_Clinic.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.Reports.TrendAnalysis
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly PdfReportService _pdfReportService;

        public IndexModel(AppDbContext context)
        {
            _context = context;
            _pdfReportService = new PdfReportService();
        }

        public int TotalConsultations { get; set; }

        public int PatientsServed { get; set; }

        public int HealthAlerts { get; set; }

        public double AverageHeartRate { get; set; }
        public List<string> MonthLabels { get; set; } = new();

        public List<int> MonthConsultationCounts { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public int ActiveAlerts { get; set; }

        public int AcknowledgedAlerts { get; set; }

        public int ResolvedAlerts { get; set; }
        public List<string> VisitReasonLabels { get; set; } = new();

        public List<int> VisitReasonCounts { get; set; } = new();

        public async Task OnGetAsync()
        {
            TotalConsultations = await _context.MedicalRecords.CountAsync();

            PatientsServed = await _context.MedicalRecords
                .Select(m => m.StudentId)
                .Distinct()
                .CountAsync(); ;

            HealthAlerts = await _context.Alerts.CountAsync();
            ActiveAlerts = await _context.Alerts
            .CountAsync(a => a.Status == "Active");

            AcknowledgedAlerts = await _context.Alerts
                .CountAsync(a => a.Status == "Acknowledged");

            ResolvedAlerts = await _context.Alerts
                .CountAsync(a => a.Status == "Resolved");

            AverageHeartRate = await _context.VitalSignLogs
                .AverageAsync(v => (double?)v.HeartRate) ?? 0;

            var monthlyData = await _context.MedicalRecords
                .GroupBy(m => new
                {
                     m.VisitDate.Year,
                     m.VisitDate.Month
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = new DateTime(
                        g.Key.Year,
                        g.Key.Month,
                        1
                     ).ToString("MMM yyyy"),

                     Count = g.Count()
                })
                .ToListAsync();

            MonthLabels = monthlyData.Select(x => x.Month).ToList();

            MonthConsultationCounts = monthlyData.Select(x => x.Count).ToList();

            // Top Visit Reasons
            var visitReasonData = await _context.VitalSignLogs
                .Where(v => !string.IsNullOrWhiteSpace(v.VisitReason))
                .GroupBy(v => v.VisitReason)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new
                {
                    Reason = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            VisitReasonLabels = visitReasonData
                .Select(x => x.Reason)
                .ToList();

            VisitReasonCounts = visitReasonData
                .Select(x => x.Count)
                .ToList();

            Insights.Clear();

            // Consultations
            if (TotalConsultations >= 20)
            {
                Insights.Add("Clinic utilization is high during the selected period, indicating frequent use of healthcare services.");
            }
            else if (TotalConsultations >= 10)
            {
                Insights.Add("Clinic utilization is within the expected range.");
            }
            else
            {
                Insights.Add("Clinic utilization is relatively low this period.");
            }

            // Patients
            Insights.Add($"{PatientsServed} unique students have visited the clinic.");

            // Alerts
            if (HealthAlerts >= 15)
            {
                Insights.Add("A significant number of health alerts were generated. Review recurring cases for possible interventions.");
            }
            else if (HealthAlerts > 0)
            {
                Insights.Add("Health alerts are being monitored and remain manageable.");
            }
            else
            {
                Insights.Add("No health alerts have been generated.");
            }

            // Heart Rate
            if (AverageHeartRate >= 60 && AverageHeartRate <= 100)
            {
                Insights.Add($"Average heart rate ({AverageHeartRate:F1} BPM) is within the normal adult range.");
            }
            else
            {
                Insights.Add($"Average heart rate ({AverageHeartRate:F1} BPM) falls outside the normal range.");
            }

            // Consultation Frequency
            double consultationsPerStudent =
                PatientsServed > 0
                    ? (double)TotalConsultations / PatientsServed
                    : 0;

            Insights.Add($"On average, each student visited the clinic {consultationsPerStudent:F1} times during the selected period.");
        }
        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            await OnGetAsync();

            var pdf = _pdfReportService.GenerateTrendAnalysisReport(
                TotalConsultations,
                PatientsServed,
                HealthAlerts,
                AverageHeartRate,
                Insights);

            return File(
                pdf,
                "application/pdf",
                $"TrendAnalysisReport_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
