using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MonitorPedidos.Web.Areas.Identity.Pages;

[Authorize]
public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}
