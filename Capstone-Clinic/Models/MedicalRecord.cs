namespace Capstone_Clinic.Models
{
    public class MedicalRecord
    {
        public int MedicalRecordId { get; set; }

        public int StudentId { get; set; }

        public DateTime VisitDate { get; set; }

        public string ChiefComplaint { get; set; } = "";

        public string Diagnosis { get; set; } = "";

        public string Medications { get; set; } = "";

        public string Allergies { get; set; } = "";

        public string ClinicalNotes { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}