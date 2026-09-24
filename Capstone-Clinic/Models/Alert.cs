namespace Capstone_Clinic.Models
{
    public class Alert
    {
        public int AlertId { get; set; }

        // The vital-sign record that triggered this alert
        public int VitalLogId { get; set; }
        public VitalSignLog? VitalSignLog { get; set; }

        // Example: Tachycardia, Bradycardia, Hypoxemia
        public string AlertType { get; set; } = "";

        // Description of the abnormal vital-sign reading
        public string AlertMessage { get; set; } = "";

        // Active, Acknowledged, or Resolved
        public string Status { get; set; } = "Active";

        // Date and time the alert was generated
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}