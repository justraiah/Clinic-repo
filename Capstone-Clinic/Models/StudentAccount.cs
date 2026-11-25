using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Models
{
    [Index(nameof(StudentId), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class StudentAccount
    {
        public int StudentAccountId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = Capstone_Clinic.Helpers.TimeHelper.Now;

        public DateTime UpdatedAt { get; set; } = Capstone_Clinic.Helpers.TimeHelper.Now;

        // Navigation property
        public Student? Student { get; set; }
    }
}