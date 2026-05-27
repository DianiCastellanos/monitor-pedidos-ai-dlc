using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MonitorPedidos.Domain.Identity;
using System.Security.Claims;

namespace MonitorPedidos.Web.Areas.Identity.Pages;

[AllowAnonymous]
public class SelectModel(ILogger<SelectModel> logger) : PageModel
{
    public string? ReturnUrl { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/dashboard");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string identity, string? returnUrl = null)
    {
        var selected = identity switch
        {
            "Operador" => PredefinedIdentities.AnalistaOperativo,
            "Tecnico"  => PredefinedIdentities.ResponsableTecnico,
            _          => null
        };

        if (selected is null)
        {
            ReturnUrl = returnUrl;
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, selected.DisplayName),
            new(ClaimTypes.Role, selected.RoleName)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties { IsPersistent = false });

        logger.LogInformation("identidad_seleccionada | rol={Role}", selected.RoleName);

        return LocalRedirect(returnUrl ?? "/dashboard");
    }
}
