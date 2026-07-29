using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Capstone_Clinic.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Capstone_Clinic.Pages;

public class LoginModel : PageModel
{
    private readonly AppDbContext _context;

    public LoginModel(AppDbContext context)
    {
        _context = context;
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
        var staff = await _context.Staffs
            .FirstOrDefaultAsync(s =>
                s.Username == Username &&
                s.Password == Password);

        if (staff == null)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, staff.FullName),
        new Claim(ClaimTypes.NameIdentifier, staff.StaffId.ToString()),
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