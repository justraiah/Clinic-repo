using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Capstone_Clinic.Services
{
    public class PdfReportService
    {
        public byte[] GenerateTrendAnalysisReport(
    int totalConsultations,
    int patientsServed,
    int healthAlerts,
    double averageHeartRate,
    List<string> insights)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header()
    .Column(column =>
    {
        column.Spacing(6);

        column.Item()
            .Background(Colors.Blue.Medium)
            .Padding(12)
            .Text("Clinic Monitoring System")
            .FontColor(Colors.White)
            .FontSize(24)
            .Bold();

        column.Item().Text("Trend Analysis Report")
            .FontSize(18)
            .Bold();

        column.Item().Text("Healthcare Analytics and Clinic Insights")
            .FontSize(11)
            .FontColor(Colors.Grey.Darken1);

        column.Item().LineHorizontal(1);

        column.Item().AlignRight()
            .Text($"Generated: {DateTime.Now:MMMM dd, yyyy • hh:mm tt}")
            .FontSize(10)
            .FontColor(Colors.Grey.Darken2);
    });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            column.Item()
    .Background(Colors.Grey.Lighten3)
    .Padding(12)
    .Column(summary =>
    {
        summary.Spacing(10);

        summary.Item()
            .Text("SUMMARY")
            .FontSize(16)
            .Bold()
            .FontColor(Colors.Blue.Darken2);

        summary.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
            });

            void Row(string label, string value)
            {
                table.Cell()
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(6)
                    .Text(label)
                    .SemiBold();

                table.Cell()
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(6)
                    .AlignRight()
                    .Text(value)
                    .Bold();
            }

            Row("Total Consultations", totalConsultations.ToString());
            Row("Patients Served", patientsServed.ToString());
            Row("Health Alerts", healthAlerts.ToString());
            Row("Average Heart Rate", $"{averageHeartRate:F1} BPM");
        });
    });

                            column.Item().PaddingTop(15);

                            column.Item()
    .PaddingTop(20)
    .Column(section =>
    {
        section.Spacing(8);

        section.Item()
            .Text("KEY INSIGHTS")
            .FontSize(16)
            .Bold()
            .FontColor(Colors.Blue.Darken2);

        foreach (var insight in insights)
        {
            section.Item()
                .Row(row =>
                {
                    row.ConstantItem(18)
                        .Text("✓")
                        .FontColor(Colors.Green.Darken2)
                        .Bold();

                    row.RelativeItem()
                        .Text(insight)
                        .FontSize(11);
                });
        }
    });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Clinic Monitoring System • Page ");
                            text.CurrentPageNumber();
                        });
                });
            }).GeneratePdf();
        }
    }
}