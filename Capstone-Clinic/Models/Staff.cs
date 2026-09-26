using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models;

public class Staff
{
    [Key]
    public int StaffId { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = "";

    [StringLength(255)]
    public string? PasswordHash { get; set; } = "";

    [Required]
    [StringLength(30)]
    public string Role { get; set; } = "";
}