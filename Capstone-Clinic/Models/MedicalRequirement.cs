using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models;

public class MedicalRequirement
{
    public int MedicalRequirementId { get; set; }

    public int StudentId { get; set; }

    public Student? Student { get; set; }

    [Required]
    [StringLength(100)]
    public string RequirementName { get; set; } = "";

    [Required]
    [RegularExpression("^(Not Submitted|Complete)$",
    ErrorMessage = "Status must be Not Submitted or Complete.")]
    [StringLength(30)]
    public string Status { get; set; } = "Not Submitted";
}