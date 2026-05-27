using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MonitorPedidos.Web.Areas.Identity.Pages;

[Authorize]
public class LogoutModel(ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "desconocido";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        logger.LogInformation("sesion_cerrada | rol={Role}", role);
        return RedirectToPage("/Identity/Select", new { area = "Identity" });
    }
}
