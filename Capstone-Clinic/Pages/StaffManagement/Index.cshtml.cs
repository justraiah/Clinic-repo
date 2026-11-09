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

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<Staff> StaffAccounts { get; private set; } = new List<Staff>();

    [BindProperty]
    public CreateClinicAccountInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadStaffAccountsAsync();
    }

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
        var usernameExists = await _context.Staffs
            .AnyAsync(s => s.Username.ToUpper() == normalizedUsername);

        if (usernameExists)
        {
            ModelState.AddModelError("Input.Username", "That username is already in use.");
            await LoadStaffAccountsAsync();
            return Page();
        }

        var staff = new Staff
        {
            FullName = Input.FullName,
            Username = Input.Username,
            Role = StaffRoles.ClinicPersonnel
        };

        staff.PasswordHash = _passwordHasher.HashPassword(staff, Input.Password);

        _context.Staffs.Add(staff);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Clinic personnel account created for {staff.FullName}.";
        return RedirectToPage("/StaffManagement/Index");
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
                Role = s.Role
            })
            .ToListAsync();
    }

    public sealed class CreateClinicAccountInput
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(50, MinimumLength = 3)]
        [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Use only letters, numbers, dots, underscores, or hyphens.")]
        public string Username { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 12, ErrorMessage = "Use a password with at least 12 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
