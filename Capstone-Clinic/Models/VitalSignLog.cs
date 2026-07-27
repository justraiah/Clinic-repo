namespace Capstone_Clinic.Models
{
    public class VitalSignLog
    {
        public int VitalSignLogId { get; set; }

        public int MedicalRecordId { get; set; }

        public double Temperature { get; set; }

        public int HeartRate { get; set; }

        public int OxygenSaturation { get; set; }

        public int SystolicBP { get; set; }

        public int DiastolicBP { get; set; }

        public DateTime RecordedAt { get; set; }

        public int StaffId { get; set; }
        public string Status { get; set; } = "Normal";

        public string Remarks { get; set; } = "";
    }
}