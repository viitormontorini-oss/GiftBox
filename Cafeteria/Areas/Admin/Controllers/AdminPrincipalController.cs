using Microsoft.AspNetCore.Mvc;

namespace Cafeteria.Areas.Admin.Controllers;

[Route("Admin")]
public class AdminPrincipalController : AdminBaseController
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index() => View();

    [HttpGet("AcessoNegado")]
    public IActionResult AcessoNegado()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
