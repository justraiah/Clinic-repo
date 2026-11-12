using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Capstone_Clinic.Models.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages.StaffManagement;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Staff> _passwordHasher = new();

    public IndexModel(AppDbContext context) => _context = context;

    public IList<Staff> StaffAccounts { get; private set; } = new List<Staff>();

    [BindProperty]
    public CreateClinicAccountInput Input { get; set; } = new();

    public async Task OnGetAsync() => await LoadStaffAccountsAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.FullName = Input.FullName?.Trim() ?? string.Empty;
        Input.Username = Input.Username?.Trim() ?? string.Empty;

        if (!ModelState.IsValid)
        {
            await LoadStaffAccountsAsync();
            return Page();
        }

        var normalizedUsername = Input.Username.ToUpperInvariant();
        if (await _context.Staffs.AnyAsync(s => s.Username.ToUpper() == normalizedUsername))
        {
            ModelState.AddModelError("Input.Username", "That username is already in use.");
            await LoadStaffAccountsAsync();
            return Page();
        }

        var staff = new Staff
        {
            FullName = Input.FullName,
            Username = Input.Username,
            Role = StaffRoles.ClinicPersonnel,
            IsActive = true
        };
        staff.PasswordHash = _passwordHasher.HashPassword(staff, Input.Password);

        _context.Staffs.Add(staff);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Clinic personnel account created for {staff.FullName}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int staffId, string fullName)
    {
        fullName = (fullName ?? string.Empty).Trim();
        if (fullName.Length is < 2 or > 100)
        {
            TempData["ErrorMessage"] = "A staff name must contain between 2 and 100 characters.";
            return RedirectToPage();
        }

        var staff = await _context.Staffs.FindAsync(staffId);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Staff account was not found.";
            return RedirectToPage();
        }

        staff.FullName = fullName;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Staff details updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(int staffId)
    {
        var staff = await _context.Staffs.SingleOrDefaultAsync(s => s.StaffId == staffId);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Staff account was not found.";
            return RedirectToPage();
        }

        var currentStaffId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (staff.IsActive && currentStaffId == staff.StaffId.ToString())
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account while signed in.";
            return RedirectToPage();
        }

        if (staff.IsActive && staff.Role == StaffRoles.Admin)
        {
            var activeAdminCount = await _context.Staffs
                .CountAsync(s => s.Role == StaffRoles.Admin && s.IsActive);
            if (activeAdminCount <= 1)
            {
                TempData["ErrorMessage"] = "The last active administrator cannot be deactivated.";
                return RedirectToPage();
            }
        }

        staff.IsActive = !staff.IsActive;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = staff.IsActive
            ? $"{staff.FullName}'s account has been activated."
            : $"{staff.FullName}'s account has been deactivated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(int staffId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 12 || newPassword.Length > 100)
        {
            TempData["ErrorMessage"] = "The new password must contain between 12 and 100 characters.";
            return RedirectToPage();
        }

        var staff = await _context.Staffs.SingleOrDefaultAsync(s => s.StaffId == staffId);
        if (staff == null)
        {
            TempData["ErrorMessage"] = "Staff account was not found.";
            return RedirectToPage();
        }

        staff.PasswordHash = _passwordHasher.HashPassword(staff, newPassword);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Password reset for {staff.FullName}. Share the temporary password securely.";
        return RedirectToPage();
    }

    private async Task LoadStaffAccountsAsync()
    {
        StaffAccounts = await _context.Staffs
            .AsNoTracking()
            .OrderBy(s => s.FullName)
            .Select(s => new Staff
            {
                StaffId = s.StaffId,
                FullName = s.FullName,
                Username = s.Username,
                Role = s.Role,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public sealed class CreateClinicAccountInput
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Use only letters, numbers, dots, underscores, or hyphens.")]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 12, ErrorMessage = "Use a password with at least 12 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
