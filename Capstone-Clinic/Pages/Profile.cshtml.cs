
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Staff> _passwordHasher = new();

    public ProfileModel(AppDbContext context)
    {
        _context = context;
    }

    public Staff? CurrentStaff { get; private set; }

    [BindProperty]
    public ChangePasswordInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        return await LoadProfileAsync() ? Page() : RedirectToPage("/Login");
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadProfileAsync();
            return Page();
        }

        var staffIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(staffIdClaim, out var staffId))
            return RedirectToPage("/Login");

        var staff = await _context.Staffs
            .SingleOrDefaultAsync(s => s.StaffId == staffId && s.IsActive);

        if (staff == null || string.IsNullOrWhiteSpace(staff.PasswordHash))
            return RedirectToPage("/Login");

        var verification = _passwordHasher.VerifyHashedPassword(
            staff,
            staff.PasswordHash,
            Input.CurrentPassword);

        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                "Input.CurrentPassword",
                "Your current password is incorrect.");

            await LoadProfileAsync();
            return Page();
        }

        if (_passwordHasher.VerifyHashedPassword(
            staff,
            staff.PasswordHash,
            Input.NewPassword) != PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(
                "Input.NewPassword",
                "Your new password must be different from your current password.");

            await LoadProfileAsync();
            return Page();
        }

        staff.PasswordHash = _passwordHasher.HashPassword(
            staff,
            Input.NewPassword);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your password has been changed successfully.";

        return RedirectToPage();
    }

    private async Task<bool> LoadProfileAsync()
    {
        var staffIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(staffIdClaim, out var staffId))
            return false;

        CurrentStaff = await _context.Staffs
            .AsNoTracking()
            .Where(s => s.StaffId == staffId && s.IsActive)
            .Select(s => new Staff
            {
                StaffId = s.StaffId,
                FullName = s.FullName,
                Username = s.Username,
                Role = s.Role
            })
            .SingleOrDefaultAsync();

        return CurrentStaff != null;
    }

    public sealed class ChangePasswordInput
    {
        [Required(ErrorMessage = "Enter your current password.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 12,
            ErrorMessage = "Your new password must contain 12 to 100 characters.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword),
            ErrorMessage = "The password confirmation does not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
