
using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models;

public class DentalRecord
{
    public int DentalRecordId { get; set; }

    [Required]
    public int ClinicPatientId { get; set; }

    public ClinicPatient? ClinicPatient { get; set; }

    [Required(ErrorMessage = "Visit date is required.")]
    public DateTime VisitDate { get; set; }

    [Required(ErrorMessage = "Chief complaint is required.")]
    public string ChiefComplaint { get; set; } = "";

    public string? DentalFindings { get; set; }

    public string? Diagnosis { get; set; }

    public string? TreatmentProvided { get; set; }

    public string? Medications { get; set; }

    public string? ClinicalNotes { get; set; }

    public DateTime CreatedAt { get; set; }
}
