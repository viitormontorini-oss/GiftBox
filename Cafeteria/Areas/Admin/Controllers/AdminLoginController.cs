using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cafeteria.Areas.Admin.Controllers;

// Login único: o gestor entra pela mesma tela do cliente (/Conta/Login, no ContaController).
// Aqui sobrou só o endereço antigo /Admin/Login (redireciona) e o Sair do painel.
[Route("Admin")]
public class AdminLoginController : AdminBaseController
{
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "AdminPrincipal");
        // area = "" porque o ContaController fica fora da Area Admin
        return RedirectToAction("Login", "Conta", new { area = "" });
    }

    [HttpPost("Logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Conta", new { area = "" });   // volta para a tela única
    }
}
