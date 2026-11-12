using Services.Models;
using System.Security.Claims;
using Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.EF;
using Repository.Repositories;
using Services;

namespace Cafeteria.Areas.Admin.Controllers;

// só o nível Administrador entra aqui
[Authorize(Roles = "Administrador")]
[Route("Admin/Usuarios")]
public class AdminUsuariosController : AdminBaseController
{
    private readonly UsuarioServices _usuarios;
    public AdminUsuariosController(Context context)
    {
        _usuarios = new UsuarioServices(new UsuarioRepository(context));
    }

    [HttpGet("")]
    public IActionResult Index() => View(_usuarios.GetAll().OrderBy(u => u.nome).ToList());

    [HttpGet("Cadastrar")]
    public IActionResult Cadastrar() => View(new Usuario { nivel = "Gestor" });

    [HttpPost("Cadastrar")]
    public IActionResult Cadastrar([Bind("nome,email,login,senha,nivel")] Usuario usuario)
    {
        if (ModelState.IsValid && _usuarios.Insert(usuario))
        {
            TempData["Sucesso"] = "Usuário cadastrado com sucesso.";
            return RedirectToAction("Index");
        }
        usuario.senha = "";
        CopiarValidacoes(_usuarios.Validation);
        return View(usuario);
    }
    [HttpGet("Ver/{id:int}")]
    public IActionResult Ver(int id) { var usuario = _usuarios.GetById(id); return usuario == null ? NotFound() : View(usuario); }
    [HttpGet("Editar/{id:int}")]
    public IActionResult Editar(int id)
    {
        var usuario = _usuarios.GetById(id);
        if (usuario == null) return NotFound();
        ViewData["Id"] = id;
        return View(new UsuarioEdicao { Nome = usuario.nome, Email = usuario.email, Login = usuario.login, Nivel = usuario.nivel });
    }
    [HttpPost("Editar/{id:int}")]
    public IActionResult Editar(int id, UsuarioEdicao dados)
    {
        if (_usuarios.GetById(id) == null) return NotFound();
        if (ModelState.IsValid && _usuarios.Editar(id, dados))
        { TempData["Sucesso"] = "Usuário atualizado."; return RedirectToAction("Index"); }
        CopiarValidacoes(_usuarios.Validation); dados.NovaSenha = ""; ViewData["Id"] = id;
        return View(dados);
    }
    [HttpGet("Excluir/{id:int}")]
    public IActionResult Excluir(int id)
    {
        var usuario = _usuarios.GetById(id);
        return usuario == null ? NotFound() : ConfirmarExclusao(id, usuario.nome);
    }
    [HttpPost("Excluir/{id:int}")]
    [ActionName("Excluir")]
    public IActionResult ExcluirConfirmado(int id)
    {
        var usuario = _usuarios.GetById(id);
        if (usuario == null) return NotFound();
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var atual);
        if (_usuarios.Excluir(id, atual)) { TempData["Sucesso"] = "Usuário excluído."; return RedirectToAction("Index"); }
        CopiarValidacoes(_usuarios.Validation);
        return ConfirmarExclusao(id, usuario.nome);
    }
}
