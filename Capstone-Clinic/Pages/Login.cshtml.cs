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
        var username = Username.Trim().ToLowerInvariant();

        lock (FailedLoginLock)
        {
            if (FailedLogins.TryGetValue(username, out var attemptInfo) &&
                attemptInfo.LockedUntil > DateTime.UtcNow)
            {
                ErrorMessage = "Too many failed login attempts. Please try again later.";
                return Page();
            }

            if (attemptInfo.LockedUntil <= DateTime.UtcNow &&
                attemptInfo.Attempts >= MaxFailedAttempts)
            {
                FailedLogins.Remove(username);
            }
        }

        var staff = await _context.Staffs
            .FirstOrDefaultAsync(s => s.Username == Username);

        var passwordValid = false;

        if (staff != null &&
            !string.IsNullOrWhiteSpace(staff.PasswordHash))
        {
            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    staff,
                    staff.PasswordHash,
                    Password);

            passwordValid =
                passwordResult != PasswordVerificationResult.Failed;
        }

        if (!passwordValid)
        {
            lock (FailedLoginLock)
            {
                var attempts = FailedLogins.TryGetValue(
                    username,
                    out var existing)
                    ? existing.Attempts + 1
                    : 1;

                var lockedUntil =
                    attempts >= MaxFailedAttempts
                        ? DateTime.UtcNow.Add(LockoutDuration)
                        : DateTime.MinValue;

                FailedLogins[username] =
                    (attempts, lockedUntil);
            }

            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        lock (FailedLoginLock)
        {
            FailedLogins.Remove(username);
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