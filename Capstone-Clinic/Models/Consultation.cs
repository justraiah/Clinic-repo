namespace Capstone_Clinic.Models
{
    public class Consultation
    {
        public int ConsultationId { get; set; }

        public int MedicalRecordId { get; set; }

        public DateTime ConsultationDate { get; set; } = DateTime.Now;

        public string ChiefComplaint { get; set; } = "";

        public string Assessment { get; set; } = "";

        public string Diagnosis { get; set; } = "";

        public string Treatment { get; set; } = "";

        public string Prescription { get; set; } = "";

        public string Recommendations { get; set; } = "";

        public int StaffId { get; set; }

        // Navigation Properties
        public MedicalRecord? MedicalRecord { get; set; }

        public Staff? Staff { get; set; }
    }
}