using Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.EF;
using Repository.Repositories;
using Services;

namespace Cafeteria.Areas.Admin.Controllers;

// só o nível Administrador entra aqui
[Authorize(Roles = "Administrador")]
[Route("Admin/CidadesEntrega")]
public class AdminCidadesEntregaController : AdminBaseController
{
    private readonly CidadeEntregaServices _cidades;
    public AdminCidadesEntregaController(Context context) => _cidades = new CidadeEntregaServices(new CidadeEntregaRepository(context));

    [HttpGet("")]
    public IActionResult Index() => View(_cidades.GetAll().OrderBy(c => c.nome).ToList());

    [HttpGet("Cadastrar")]
    public IActionResult Cadastrar() => View(new CidadeEntrega());

    [HttpPost("Cadastrar")]
    public IActionResult Cadastrar([Bind("nome,taxa,ativa")] CidadeEntrega cidade)
    {
        if (ModelState.IsValid && _cidades.Insert(cidade))
        {
            TempData["Sucesso"] = "Cidade de entrega cadastrada.";
            return RedirectToAction("Index");
        }
        CopiarValidacoes(_cidades.Validation);
        return View(cidade);
    }

    [HttpGet("Editar/{id:int}")]
    public IActionResult Editar(int id)
    {
        var cidade = _cidades.GetById(id);
        return cidade == null ? NotFound() : View(cidade);
    }

    [HttpPost("Editar/{id:int}")]
    public IActionResult Editar(int id, [Bind("nome,taxa,ativa")] CidadeEntrega dados)
    {
        var cidade = _cidades.GetById(id);
        if (cidade == null) return NotFound();
        cidade.nome = dados.nome; cidade.taxa = dados.taxa; cidade.ativa = dados.ativa;
        if (ModelState.IsValid && _cidades.Update(cidade))
        {
            TempData["Sucesso"] = "Cidade de entrega atualizada.";
            return RedirectToAction("Index");
        }
        CopiarValidacoes(_cidades.Validation);
        return View(cidade);
    }
}
