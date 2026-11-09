
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Capstone_Clinic.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        return await SignOutAndRedirectAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SignOutAndRedirectAsync();
    }

    private async Task<IActionResult> SignOutAndRedirectAsync()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToPage("/Login");
    }
}
