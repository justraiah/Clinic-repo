using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models;

[Index(nameof(StudentId), IsUnique = true)]
public class ClinicPatient
{
    public int ClinicPatientId { get; set; }

    [Required]
    [StringLength(30)]
    public string PatientType { get; set; } = "Student";

    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    [StringLength(150)]
    public string? FullName { get; set; }

    [StringLength(50)]
    public string? Identifier { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
}