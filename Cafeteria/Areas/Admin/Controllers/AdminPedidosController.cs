using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Repository.EF;
using Repository.Repositories;
using Services;
using Services.Models;
using System.Security.Claims;

namespace Cafeteria.Areas.Admin.Controllers;

[Route("Admin/Pedidos")]
public class AdminPedidosController : AdminBaseController
{
    private readonly PedidoServices _pedidos;
    private readonly ClienteRepository _clientes;
    private readonly ProdutoRepository _produtos;
    private readonly CidadeEntregaRepository _cidades;

    public AdminPedidosController(Context context)
    {
        _clientes = new ClienteRepository(context);
        _produtos = new ProdutoRepository(context);
        _cidades = new CidadeEntregaRepository(context);
        _pedidos = new PedidoServices(new PedidoRepository(context), _clientes, _produtos,
            new UsuarioRepository(context), _cidades);
    }

    [HttpGet("")]
    public IActionResult Index() => View(_pedidos.GetAll());

    [HttpGet("Cadastrar")]
    public IActionResult Cadastrar()
    {
        CarregarOpcoes();
        return View(new PedidoCadastro());
    }

    [HttpPost("Cadastrar")]
    public IActionResult Cadastrar(PedidoCadastro dados)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId);
        if (ModelState.IsValid && _pedidos.Cadastrar(dados, usuarioId))
        {
            TempData["Sucesso"] = "Pedido cadastrado com sucesso.";
            return RedirectToAction("Index");
        }
        CopiarValidacoes(_pedidos.Validation);
        if (dados.Itens == null || dados.Itens.Count == 0) dados.Itens = new() { new() };
        CarregarOpcoes();
        return View(dados);
    }

    private void CarregarOpcoes(int? cidadeAtual = null, decimal? taxaSalva = null)
    {
        ViewBag.Clientes = new SelectList(_clientes.getAll().OrderBy(c => c.nome), "id", "nome");
        ViewBag.Produtos = new SelectList(_produtos.getAll().OrderBy(p => p.nome), "id", "nome");
        ViewBag.CidadesEntrega = _cidades.getAll().Where(c => c.ativa || c.id == cidadeAtual).OrderBy(c => c.nome)
            .Select(c => new Business.CidadeEntrega { id = c.id, nome = c.nome + (!c.ativa ? " (inativa, pedido existente)" : ""),
                taxa = c.id == cidadeAtual && taxaSalva.HasValue ? taxaSalva.Value : c.taxa, ativa = c.ativa }).ToList();
    }
    [HttpGet("Ver/{id:int}")]
    public IActionResult Ver(int id) { var pedido = _pedidos.GetById(id); return pedido == null ? NotFound() : View(pedido); }
    [HttpGet("Editar/{id:int}")]
    public IActionResult Editar(int id)
    {
        var pedido = _pedidos.GetById(id);
        if (pedido == null) return NotFound();
        var cidadeId = pedido.cidade_entrega_id ?? (pedido.endereco == null ? null : _cidades.getAll().FirstOrDefault(c => c.nome == pedido.endereco.cidade)?.id);
        ViewData["Id"] = id;
        ViewData["Pedido"] = pedido; // a sua tela de editar mostra número, data e total do pedido
        CarregarOpcoes(cidadeId, pedido.taxa_entrega);
        return View(new PedidoCadastro
        {
            ClienteId = pedido.cliente.id, FormaRecebimento = pedido.endereco == null ? "Retirada" : "Entrega",
            CidadeEntregaId = cidadeId, TipoPagamento = pedido.tipo_pagamento, StatusPedido = pedido.status_pedido,
            CartaoTexto = pedido.cartao_texto, Observacao = pedido.observacao,
            Itens = pedido.produtos.Select(i => new PedidoItemCadastro { ItemId = i.id, ProdutoId = i.produto.id,
                Quantidade = i.quantidade, Decoracao = i.DescricaoCoresDecoracao }).ToList(),
            Endereco = pedido.endereco == null ? new() : new() { Logradouro = pedido.endereco.logradouro, Numero = pedido.endereco.numero,
                Bairro = pedido.endereco.bairro, Cidade = pedido.endereco.cidade, Cep = pedido.endereco.cep, Complemento = pedido.endereco.complemento }
        });
    }
    [HttpPost("Editar/{id:int}")]
    public IActionResult Editar(int id, PedidoCadastro dados)
    {
        var pedido = _pedidos.GetById(id);
        if (pedido == null) return NotFound();
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId);
        if (ModelState.IsValid && _pedidos.Editar(id, dados, usuarioId))
        { TempData["Sucesso"] = "Pedido atualizado."; return RedirectToAction("Index"); }
        CopiarValidacoes(_pedidos.Validation); ViewData["Id"] = id; ViewData["Pedido"] = pedido;
        if (dados.Itens == null || dados.Itens.Count == 0) dados.Itens = new() { new() };
        CarregarOpcoes(pedido.cidade_entrega_id, pedido.taxa_entrega);
        return View(dados);
    }
    // a confirmação é a sua tela de excluir (com o modal), não a tela comum do Yago
    [HttpGet("Excluir/{id:int}")]
    public IActionResult Excluir(int id)
    {
        var pedido = _pedidos.GetById(id);
        return pedido == null ? NotFound() : View(pedido);
    }
    [HttpPost("Excluir/{id:int}")]
    [ActionName("Excluir")]
    public IActionResult ExcluirConfirmado(int id)
    {
        var pedido = _pedidos.GetById(id);
        if (pedido == null) return NotFound();
        if (_pedidos.Excluir(id)) { TempData["Sucesso"] = "Pedido excluído."; return RedirectToAction("Index"); }
        CopiarValidacoes(_pedidos.Validation);
        return View(pedido);
    }
}
