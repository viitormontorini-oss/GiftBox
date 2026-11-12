using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Cafeteria.Infrastructure;

public class UsuarioCookieEvents(Context context) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext cookie)
    {
        int.TryParse(cookie.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var usuario = id > 0 ? await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.id == id) : null;
        if (usuario == null)
        {
            cookie.RejectPrincipal();
            await cookie.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }
        if (cookie.Principal!.Identity?.Name != usuario.nome || !cookie.Principal.IsInRole(usuario.nivel))
        {
            cookie.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                new Claim(ClaimTypes.Name, usuario.nome), new Claim(ClaimTypes.Role, usuario.nivel)
            }, CookieAuthenticationDefaults.AuthenticationScheme)));
            cookie.ShouldRenew = true;
        }
    }
}
