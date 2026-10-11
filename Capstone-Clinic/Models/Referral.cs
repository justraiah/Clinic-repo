using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models
{
    public class Referral
    {
        public int ReferralId { get; set; }

        [Required]
        public int ClinicPatientId { get; set; }

        public ClinicPatient? ClinicPatient { get; set; }

        [Required(ErrorMessage = "Referral date is required.")]
        public DateTime ReferralDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Destination facility is required.")]
        [StringLength(200)]
        public string FacilityName { get; set; } = "";

        [Required(ErrorMessage = "Reason for referral is required.")]
        public string ReasonForReferral { get; set; } = "";

        public string? ClinicalNotes { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}