using Microsoft.AspNetCore.Mvc;
using Repository.EF;
using Repository.Repositories;
using Services;
using Services.Models;
using Cafeteria.Infrastructure;

namespace Cafeteria.Areas.Admin.Controllers;

[Route("Admin/Produtos")]
public class AdminProdutosController : AdminBaseController
{
    private readonly ProdutoServices _produtos;
    private readonly CategoriaServices _categorias;  // para listar as categorias no cadastro
    private readonly IWebHostEnvironment _environment;
    public AdminProdutosController(Context context, IWebHostEnvironment environment)
    {
        _produtos = new ProdutoServices(new ProdutoRepository(context)); _environment = environment;
        _categorias = new CategoriaServices(new CategoriaRepository(context));  // cria o service das categorias
    }
    // busca pelo nome/descrição (o filtro Ativos/Inativos saiu da tela)
    [HttpGet("")]
    public IActionResult Index(string? busca)
    {
        var produtos = _produtos.GetAll().AsEnumerable();
        if (!string.IsNullOrWhiteSpace(busca)) produtos = produtos.Where(p => p.nome.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase) || p.descricao.Contains(busca.Trim(), StringComparison.OrdinalIgnoreCase));
        ViewBag.Busca = busca;
        return View(produtos.OrderBy(p => p.nome).ToList());
    }
    [HttpGet("Cadastrar")]
    public IActionResult Cadastrar()
    {
        ViewBag.Categorias = _categorias.GetAll();  // a tela usa esta lista na caixa de categoria
        return View(new ProdutoCadastro { Preco = 0, Quantidade = 0 });
    }
    // "outro" vem do seu botão "Salvar e cadastrar outro" (volta para um cadastro em branco)
    [HttpPost("Cadastrar")]
    // fotos = as fotos extras escolhidas no campo "Outras fotos"
    public Task<IActionResult> Cadastrar(ProdutoCadastro dados, IFormFile? foto, List<IFormFile>? fotos, string? outro) => Salvar(null, dados, foto, false, fotos, null, outro != null);
    [HttpGet("Ver/{id:int}")]
    public IActionResult Ver(int id) { var produto = _produtos.GetById(id); return produto == null ? NotFound() : View(produto); }
    [HttpGet("Editar/{id:int}")]
    public IActionResult Editar(int id)
    {
        var produto = _produtos.GetById(id);
        if (produto == null) return NotFound();
        ViewData["Id"] = id; ViewData["ImagemAtual"] = produto.imagem;
        ViewBag.FotosExtras = produto.imagens;      // fotos extras, cada uma com o seu Remover
        ViewBag.Categorias = _categorias.GetAll();  // a tela usa esta lista na caixa de categoria
        return View(new ProdutoCadastro { Nome = produto.nome, Descricao = produto.descricao, Preco = (decimal)produto.preco, Quantidade = produto.quantidade, CategoriaId = produto.categoriaId });
    }
    [HttpPost("Editar/{id:int}")]
    // removerFoto = número da foto extra do botão Remover
    public Task<IActionResult> Editar(int id, ProdutoCadastro dados, IFormFile? foto, bool removerImagem, List<IFormFile>? fotos, int? removerFoto) => Salvar(id, dados, foto, removerImagem, fotos, removerFoto, false);
    private async Task<IActionResult> Salvar(int? id, ProdutoCadastro dados, IFormFile? foto, bool removerImagem, List<IFormFile>? fotos, int? removerFoto, bool cadastrarOutro)
    {
        ViewBag.Categorias = _categorias.GetAll();  // se der erro, a tela volta e precisa da lista de novo
        var atual = id.HasValue ? _produtos.GetById(id.Value) : null;
        if (id.HasValue && atual == null) return NotFound();
        ViewData["Id"] = id; ViewData["ImagemAtual"] = atual?.imagem;
        ViewBag.FotosExtras = atual?.imagens;  // se der erro, a tela de editar mostra as fotos extras de novo
        var antiga = atual?.imagem;
        // caminho da foto extra do Remover: o arquivo é apagado da pasta depois de salvar
        string? fotoRemovida = atual?.imagens.FirstOrDefault(f => f.id == removerFoto)?.arquivo;
        string? imagem = null;
        List<string> novas = new List<string>();  // caminhos das fotos extras salvas agora
        if (ModelState.IsValid && _produtos.Validar(dados))
        {
            try
            {
                imagem = await ProdutoImagemStorage.SalvarAsync(foto, _environment);
                foreach (IFormFile arquivo in fotos ?? new List<IFormFile>())                 // cada foto extra escolhida
                {
                    string? caminho = await ProdutoImagemStorage.SalvarAsync(arquivo, _environment);  // grava na pasta
                    if (caminho != null) novas.Add(caminho);                                  // guarda o caminho para o banco
                }
            }
            catch (InvalidOperationException ex) { ModelState.AddModelError("foto", ex.Message); }
            if (ModelState.IsValid)
            {
                var salvo = id.HasValue ? _produtos.Editar(id.Value, dados, imagem, removerImagem, novas, removerFoto) : _produtos.Cadastrar(dados, imagem, novas);
                if (salvo)
                {
                    if (id.HasValue && (imagem != null || removerImagem)) ProdutoImagemStorage.Remover(antiga, _environment);
                    ProdutoImagemStorage.Remover(fotoRemovida, _environment);  // apaga da pasta a foto extra removida (as de ângulo ficam)
                    TempData["Sucesso"] = id.HasValue ? "Produto atualizado." : "Produto cadastrado.";
                    return cadastrarOutro ? RedirectToAction("Cadastrar") : RedirectToAction("Index");
                }
            }
        }
        if (imagem != null) ProdutoImagemStorage.Remover(imagem, _environment);
        foreach (string caminho in novas)  // não salvou: apaga da pasta as fotos extras gravadas agora
        {
            ProdutoImagemStorage.Remover(caminho, _environment);
        }
        CopiarValidacoes(_produtos.Validation);
        return View(id.HasValue ? "Editar" : "Cadastrar", dados);
    }
    // Botão "Inativar": zera o estoque. Para reativar, o gestor abre o Editar e coloca o estoque de novo
    [HttpPost("Inativar/{id:int}")]
    public IActionResult Inativar(int id)
    {
        if (_produtos.Inativar(id))                         // o service zera o estoque e grava
        {
            TempData["Sucesso"] = "Produto inativado.";     // faixa verde na lista
            return RedirectToAction("Index");               // volta para a lista de produtos
        }

        return NotFound();                                  // o produto não existe
    }

    // a confirmação é a sua tela de excluir (com o modal)
    [HttpGet("Excluir/{id:int}")]
    public IActionResult Excluir(int id)
    {
        var produto = _produtos.GetById(id);
        return produto == null ? NotFound() : View(produto);
    }
    [HttpPost("Excluir/{id:int}")]
    [ActionName("Excluir")]
    public IActionResult ExcluirConfirmado(int id)
    {
        var produto = _produtos.GetById(id);
        if (produto == null) return NotFound();
        var imagem = produto.imagem;
        var extras = produto.imagens.Select(f => f.arquivo).ToList();  // caminhos das fotos extras, para apagar da pasta
        if (_produtos.Excluir(id))
        {
            ProdutoImagemStorage.Remover(imagem, _environment);
            foreach (string caminho in extras)  // apaga da pasta cada foto extra (as de ângulo ficam)
            {
                ProdutoImagemStorage.Remover(caminho, _environment);
            }
            TempData["Sucesso"] = "Produto excluído.";
            return RedirectToAction("Index");
        }
        CopiarValidacoes(_produtos.Validation);
        return View(produto);
    }
}
