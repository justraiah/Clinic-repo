namespace Capstone_Clinic.Models.Authorization;

/// <summary>
/// Canonical role names used by staff accounts and authorization policies.
/// Keep these values aligned with the Role column in the Staffs table.
/// </summary>
public static class StaffRoles
{
    public const string Admin = "Admin";
    public const string ClinicPersonnel = "ClinicPersonnel";
}
