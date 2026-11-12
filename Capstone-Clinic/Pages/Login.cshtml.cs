using Capstone_Clinic.Data;
using Capstone_Clinic.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Capstone_Clinic.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<Staff> _passwordHasher;
    private static readonly Dictionary<string, (int Attempts, DateTime LockedUntil)> FailedLogins = new();
    private static readonly object FailedLoginLock = new();

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public LoginModel(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<Staff>();
    }
    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string ErrorMessage { get; set; } = "";

    public void OnGet()
    {

    }

    public async Task<IActionResult> OnPostAsync()
    {
        var normalizedUsername =
            (Username ?? string.Empty).Trim().ToUpperInvariant();

        var password = Password ?? string.Empty;

        // Reject empty credentials.
        if (string.IsNullOrWhiteSpace(normalizedUsername) ||
            string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        // Check account lockout.
        lock (FailedLoginLock)
        {
            if (FailedLogins.TryGetValue(
                normalizedUsername, out var attemptInfo))
            {
                if (attemptInfo.LockedUntil > DateTime.UtcNow)
                {
                    ErrorMessage =
                        "Too many failed login attempts. Please try again later.";
                    return Page();
                }

                if (attemptInfo.Attempts >= MaxFailedAttempts)
                {
                    FailedLogins.Remove(normalizedUsername);
                }
            }
        }

        // Username lookup is case-insensitive.
        var staff = await _context.Staffs
            .FirstOrDefaultAsync(s =>
                s.Username.ToUpper() == normalizedUsername);

        var passwordResult = PasswordVerificationResult.Failed;

        if (staff != null &&
            !string.IsNullOrWhiteSpace(staff.PasswordHash))
        {
            passwordResult = _passwordHasher.VerifyHashedPassword(
                staff,
                staff.PasswordHash,
                password);
        }

        // Inactive accounts cannot authenticate.
        var passwordValid =
            staff != null &&
            staff.IsActive &&
            passwordResult != PasswordVerificationResult.Failed;

        if (!passwordValid)
        {
            lock (FailedLoginLock)
            {
                var attempts = FailedLogins.TryGetValue(
                    normalizedUsername,
                    out var existing)
                        ? existing.Attempts + 1
                        : 1;

                var lockedUntil =
                    attempts >= MaxFailedAttempts
                        ? DateTime.UtcNow.Add(LockoutDuration)
                        : DateTime.MinValue;

                FailedLogins[normalizedUsername] =
                    (attempts, lockedUntil);
            }

            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        // Upgrade old password hashes when required.
        if (passwordResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            staff!.PasswordHash =
                _passwordHasher.HashPassword(staff, password);

            await _context.SaveChangesAsync();
        }

        // Clear failed attempts after successful login.
        lock (FailedLoginLock)
        {
            FailedLogins.Remove(normalizedUsername);
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, staff!.FullName),
        new Claim(
            ClaimTypes.NameIdentifier,
            staff.StaffId.ToString()),
        new Claim(ClaimTypes.Role, staff.Role)
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);

        return RedirectToPage("/Index");
    }
}