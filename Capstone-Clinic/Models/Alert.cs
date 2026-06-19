namespace Capstone_Clinic.Models
{
    public class Alert
    {
        public int AlertId { get; set; }

        public int VitalLogId { get; set; }

        public string AlertType { get; set; } = "";

        public string AlertMessage { get; set; } = "";

        public string Status { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}
