using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Models
{
    [Index(nameof(StudentNumber), IsUnique = true)]
        public class Student
    {
        public int StudentId { get; set; }

        [Required]
        [StringLength(30)]
        public string StudentNumber { get; set; } = "";

        public string FullName { get; set; } = "";
        
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string Course { get; set; } = "";

        public string YearLevel { get; set; } = "";

        // Medical requirements do not restrict clinic services.
        public string MedicalRequirementStatus { get; set; } = "Not Submitted";
        public StudentAccount? StudentAccount { get; set; }
    }
}