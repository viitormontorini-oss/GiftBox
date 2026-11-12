using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Cafeteria.Models;
using Repository.EF;            // onde está o Context
using Repository.Repositories;  // onde estão os repositórios
using Services;                 // onde estão os services

namespace Cafeteria.Controllers;

// Todas as páginas públicas do catálogo. Segue a aula "Camada Service - Aula 5" do professor:
// o controller pega os dados no service e entrega para a tela.
public class HomeController : Controller
{
    private readonly ProdutoServices _produtos;      // regras e busca dos produtos
    private readonly CategoriaServices _categorias;  // regras e busca das categorias

    // O ASP.NET entrega o Context sozinho (ele foi registrado no Program.cs)
    public HomeController(Context context)
    {
        _produtos = new ProdutoServices(new ProdutoRepository(context));        // cria o service dos produtos
        _categorias = new CategoriaServices(new CategoriaRepository(context));  // cria o service das categorias
    }

    // HOME: mostra as novidades e o catálogo
    public IActionResult Index()
    {
        // pega os produtos que têm categoria e estoque (estoque 0 = inativado no gestor, não aparece)
        List<Business.Produto> cestas = _produtos.GetAll().Where(p => p.categoriaId != null && p.quantidade > 0).ToList();

        // Novidades = os 8 últimos cadastrados (produto novo do gestor aparece aqui sozinho)
        ViewBag.Novidades = cestas.OrderByDescending(p => p.id).Take(8).ToList();
        // Nosso catálogo = os 4 primeiros
        ViewBag.Catalogo = cestas.OrderBy(p => p.id).Take(4).ToList();
        return View();  // abre a tela Views/Home/Index.cshtml
    }

    // PÁGINA DO PRODUTO: o id vem do fim do endereço (ex.: /Home/Produto/16)
    public IActionResult Produto(int id)
    {
        Business.Produto? cesta = _produtos.GetById(id);  // procura o produto no banco
        if (cesta == null)                                // não achou (id errado)
        {
            return RedirectToAction("Index");             // volta para a home
        }

        ViewBag.Fotos = BuscarFotos(cesta);               // foto original + ângulos (se tiver)
        // "Você também pode gostar": até 4 cestas da mesma categoria, menos a própria e menos as inativas (estoque 0)
        ViewBag.Sugestoes = _produtos.GetAll().Where(p => p.categoriaId == cesta.categoriaId && p.categoriaId != null && p.id != cesta.id && p.quantidade > 0).Take(4).ToList();
        return View(cesta);                               // abre a tela e entrega o produto (na tela ele se chama Model)
    }

    // CATEGORIA: /Home/Categoria/1 = Natal, 2 = Namorados, 3 = Dia dos Pais, 4 = Dia das Mães, 5 = Aniversário, 6 = Vinhos
    public IActionResult Categoria(int id)
    {
        Business.Categoria? categoria = _categorias.GetById(id);  // procura a categoria no banco
        if (categoria == null)                                    // número que não existe
        {
            return RedirectToAction("Index");                     // volta para a home
        }

        // pega só os produtos desta categoria que têm estoque (estoque 0 = inativado no gestor)
        List<Business.Produto> cestas = _produtos.GetAll().Where(p => p.categoriaId == id && p.quantidade > 0).ToList();
        ViewBag.Id = id;                 // para o menu saber qual item fica em negrito
        ViewBag.Nome = categoria.nome;   // título da página
        return View(cestas);             // a lista de cestas vai para a tela
    }

    // TODAS AS CESTAS: /Home/TotalProduto
    // A busca do topo usa esta mesma página: /Home/TotalProduto?busca=vinho
    public IActionResult TotalProduto(string? busca)
    {
        // pega todos os produtos que têm categoria e estoque (estoque 0 = inativado no gestor)
        List<Business.Produto> todas = _produtos.GetAll().Where(p => p.categoriaId != null && p.quantidade > 0).ToList();
        ViewBag.Nome = "Todas as cestas";  // título da página

        if (!string.IsNullOrWhiteSpace(busca))  // a pessoa digitou alguma coisa na busca?
        {
            string texto = busca.Trim();        // tira os espaços das pontas
            // comparador do C#: acha um texto dentro de outro
            var comparar = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
            // IgnoreCase = não importa maiúscula ou minúscula; IgnoreNonSpace = não importa acento (coracao acha coração)
            var regras = System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace;
            // IndexOf devolve a posição do texto (-1 se não achou). || quer dizer OU: procura no nome OU na descrição
            todas = todas.Where(p => comparar.IndexOf(p.nome, texto, regras) >= 0
                                  || comparar.IndexOf(p.descricao, texto, regras) >= 0).ToList();
            ViewBag.Nome = "Resultado para \"" + texto + "\"";  // troca o título da página
        }

        return View(todas);                // a lista de cestas vai para a tela
    }

    // MOTIVO deste método: separar as fotos deixa a action Produto curta.
    // Monta a lista de fotos do produto: a principal e as extras da tabela ProdutoImagens. Máximo de 4 fotos.
    private List<string> BuscarFotos(Business.Produto cesta)
    {
        List<string> fotos = new List<string>();  // lista vazia com os caminhos das fotos
        // a foto do produto vem primeiro (?? = se não tiver foto, usa a foto padrão)
        fotos.Add(cesta.imagem ?? "~/imgs/produtos/produto.jpg");

        foreach (var extra in cesta.imagens.OrderBy(f => f.id))  // fotos extras, na ordem em que foram cadastradas
        {
            fotos.Add(extra.arquivo);                              // guarda o caminho da foto
        }

        return fotos;  // entrega a lista (de 1 a 4 fotos)
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
