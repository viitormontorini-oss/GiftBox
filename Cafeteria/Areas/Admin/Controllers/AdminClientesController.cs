using Microsoft.AspNetCore.Mvc;
using Repository.EF;
using Repository.Repositories;
using Services;
using Services.Models;

namespace Cafeteria.Areas.Admin.Controllers;

[Route("Admin/Clientes")]
public class AdminClientesController : AdminBaseController
{
    private readonly ClienteServices _clientes;
    public AdminClientesController(Context context) => _clientes = new ClienteServices(new ClienteRepository(context));

    // busca por nome, e-mail ou telefone
    [HttpGet("")]
    public IActionResult Index(string? busca)
    {
        var clientes = _clientes.GetAll().AsEnumerable();
        if (!string.IsNullOrWhiteSpace(busca)) clientes = clientes.Where(c => c.nome.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase) || c.email.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase) || c.telefones.Any(t => t.numero.Contains(busca.Trim())));
        ViewBag.Busca = busca;
        return View(clientes.OrderBy(c => c.nome).ToList());
    }

    [HttpGet("Cadastrar")]
    public IActionResult Cadastrar() => View(new ClienteCadastro());

    [HttpPost("Cadastrar")]
    public IActionResult Cadastrar(ClienteCadastro dados)
    {
        PrepararEndereco(dados);
        if (ModelState.IsValid && _clientes.Cadastrar(dados))
        {
            TempData["Sucesso"] = "Cliente cadastrado com sucesso.";
            return RedirectToAction("Index");
        }
        CopiarValidacoes(_clientes.Validation);
        dados.Senha = "";
        return View(dados);
    }
    [HttpGet("Ver/{id:int}")]
    public IActionResult Ver(int id) { var cliente = _clientes.GetById(id); return cliente == null ? NotFound() : View(cliente); }

    [HttpGet("Editar/{id:int}")]
    public IActionResult Editar(int id)
    {
        var cliente = _clientes.GetById(id);
        if (cliente == null) return NotFound();
        var endereco = cliente.Enderecos.FirstOrDefault(e => e.pedido == null);
        ViewData["Id"] = id;
        return View(new ClienteCadastro
        {
            Nome = cliente.nome, Email = cliente.email, Login = cliente.login,
            Telefone = cliente.telefones.FirstOrDefault()?.numero,
            OutroTelefone = cliente.telefones.Skip(1).FirstOrDefault()?.numero,
            InformarEndereco = endereco != null,
            Endereco = endereco == null ? new() : new() { Logradouro = endereco.logradouro, Numero = endereco.numero, Bairro = endereco.bairro,
                Cidade = endereco.cidade, Cep = endereco.cep, Complemento = endereco.complemento }
        });
    }
    [HttpPost("Editar/{id:int}")]
    public IActionResult Editar(int id, ClienteCadastro dados)
    {
        if (_clientes.GetById(id) == null) return NotFound();
        PrepararEndereco(dados);
        if (ModelState.IsValid && _clientes.Editar(id, dados))
        { TempData["Sucesso"] = "Cliente atualizado."; return RedirectToAction("Index"); }
        CopiarValidacoes(_clientes.Validation); dados.Senha = ""; ViewData["Id"] = id;
        return View(dados);
    }
    // a confirmação é a sua tela de excluir (com o modal)
    [HttpGet("Excluir/{id:int}")]
    public IActionResult Excluir(int id)
    {
        var cliente = _clientes.GetById(id);
        return cliente == null ? NotFound() : View(cliente);
    }
    [HttpPost("Excluir/{id:int}")]
    [ActionName("Excluir")]
    public IActionResult ExcluirConfirmado(int id)
    {
        var cliente = _clientes.GetById(id);
        if (cliente == null) return NotFound();
        if (_clientes.Excluir(id)) { TempData["Sucesso"] = "Cliente excluído."; return RedirectToAction("Index"); }
        CopiarValidacoes(_clientes.Validation);
        return View(cliente);
    }

    // a sua tela não tem a caixinha "Cadastrar um endereço agora" do Yago:
    // o endereço conta como informado se algum campo foi preenchido
    private static void PrepararEndereco(ClienteCadastro dados)
    {
        var e = dados.Endereco;
        dados.InformarEndereco = !string.IsNullOrWhiteSpace(e.Cep) || !string.IsNullOrWhiteSpace(e.Logradouro)
            || !string.IsNullOrWhiteSpace(e.Bairro) || !string.IsNullOrWhiteSpace(e.Numero);
    }
}
