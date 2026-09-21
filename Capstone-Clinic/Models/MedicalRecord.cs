using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models
{
    public class MedicalRecord
    {
        public int MedicalRecordId { get; set; }

        public int StudentId { get; set; }

        public int ClinicPatientId { get; set; }

        public ClinicPatient? ClinicPatient { get; set; }

        [Required(ErrorMessage = "Visit date is required.")]
        public DateTime VisitDate { get; set; }

        [Required(ErrorMessage = "Chief complaint is required.")]
        public string ChiefComplaint { get; set; } = "";

        [Required(ErrorMessage = "Diagnosis is required.")]
        public string Diagnosis { get; set; } = "";

        public string? Medications { get; set; }

        public string? Allergies { get; set; }

        public string? ClinicalNotes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        [Range(0, 10)]
        public int PainScale { get; set; }
    }
}