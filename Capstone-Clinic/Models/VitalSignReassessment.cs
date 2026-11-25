using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models
{
    public class VitalSignReassessment
    {
        public int VitalSignReassessmentId { get; set; }

        // Link to the original vital-sign/visit record
        public int VitalSignLogId { get; set; }
        public VitalSignLog? VitalSignLog { get; set; }

        // Reassessment sequence
        public int ReassessmentNumber { get; set; }

        // Reassessment timestamp
        public DateTime RecordedAt { get; set; } = Capstone_Clinic.Helpers.TimeHelper.Now;

        // Vital signs
        [Range(20.0, 50.0)]
        public double Temperature { get; set; }

        [Range(1, 300)]
        public int HeartRate { get; set; }

        public int RespiratoryRate { get; set; }

        [Range(1, 100)]
        public int OxygenSaturation { get; set; }

        [Range(20, 300)]
        public int SystolicBP { get; set; }

        [Range(20, 200)]
        public int DiastolicBP { get; set; }

        // Staff who performed the reassessment
        public int StaffId { get; set; }
        public Staff? Staff { get; set; }

        // Clinical notes for this reassessment
        public string? Remarks { get; set; }
        public string Status { get; set; } = "Normal";
    }
}