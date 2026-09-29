using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models
{
    public class VitalSignLog
    {
        public int VitalSignLogId { get; set; }

        public int MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public int ClinicPatientId { get; set; }

        public ClinicPatient? ClinicPatient { get; set; }
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();

        [Range(30.0, 45.0)]
        public double Temperature { get; set; }

        [Range(30, 220)]
        public int HeartRate { get; set; }
        [Range(1, 100)]
        public int RespiratoryRate { get; set; }

        [Range(70, 100)]
        public int OxygenSaturation { get; set; }

        [Range(50, 250)]
        public int SystolicBP { get; set; }

        [Range(30, 150)]
        public int DiastolicBP { get; set; }
        public string VisitReason { get; set; } = "";

        public DateTime RecordedAt { get; set; }

        public int StaffId { get; set; }
        public string Status { get; set; } = "Normal";

        public string Remarks { get; set; } = "";
    }
}