using Microsoft.AspNetCore.Mvc;
using Repository.EF;            // onde está o Context
using Repository.Repositories;  // onde estão os repositórios
using Services;                 // onde estão os services

namespace Cafeteria.Controllers
{
    // Carrinho guardado em um COOKIE no navegador da pessoa, sem precisar de login.
    // O texto do cookie fica assim: "7:2,12:1" = produto 7 com 2 unidades e produto 12 com 1 unidade.
    [AutoValidateAntiforgeryToken]  // todo POST precisa do token do formulário (igual ao ContaController)
    public class CarrinhoController : Controller
    {
        private readonly ProdutoServices _produtos;  // busca os produtos no banco

        public CarrinhoController(Context context)
        {
            _produtos = new ProdutoServices(new ProdutoRepository(context));  // cria o service dos produtos
        }

        // TELA DO CARRINHO: /Carrinho
        public IActionResult Index()
        {
            Dictionary<int, int> carrinho = LerCarrinho(Request.Cookies["carrinho"]);             // lê o cookie
            List<Business.Produto> cestas = new List<Business.Produto>();  // lista dos produtos que vão aparecer
            double total = 0;                                          // soma dos valores

            foreach (var item in carrinho)                             // olha cada produto do carrinho
            {
                Business.Produto? cesta = _produtos.GetById(item.Key); // item.Key = id do produto
                if (cesta != null)                                     // o produto ainda existe?
                {
                    cestas.Add(cesta);                                 // coloca na lista da tela
                    total = total + cesta.preco * item.Value;          // item.Value = quantidade
                }
            }

            ViewBag.Quantidades = carrinho;                            // a tela usa para mostrar a quantidade de cada um
            ViewBag.Total = total;                                     // a tela mostra o total

            if (cestas.Count == 0)                                     // carrinho vazio?
            {
                // sugestões da tela de carrinho vazio: as 4 cestas mais novas
                ViewBag.Sugestoes = _produtos.GetAll().Where(p => p.categoriaId != null).OrderByDescending(p => p.id).Take(4).ToList();
            }

            return View(cestas);                                       // abre a tela e entrega os produtos
        }

        // Botão "Adicionar ao carrinho" da página do produto
        [HttpPost]
        // irParaPedido vem preenchido quando o botão apertado é o "Solicitar pedido"
        public IActionResult Adicionar(int id, int quantidade, string? irParaPedido)
        {
            Dictionary<int, int> carrinho = LerCarrinho(Request.Cookies["carrinho"]);  // lê o cookie
            if (quantidade < 1)                             // quantidade inválida
            {
                quantidade = 1;                             // usa 1
            }

            if (carrinho.ContainsKey(id))                   // esse produto já está no carrinho?
            {
                carrinho[id] = carrinho[id] + quantidade;   // sim: soma a quantidade
            }
            else
            {
                carrinho[id] = quantidade;                  // não: acrescenta o produto
            }

            SalvarCarrinho(carrinho);                       // grava no cookie

            if (irParaPedido != null)                       // apertou "Solicitar pedido"?
            {
                return RedirectToAction("Personalizar", "Pedido");  // vai direto para a etapa 1
            }

            return RedirectToAction("Index");               // abre a tela do carrinho
        }

        // Botões − e + do carrinho. mudanca vale -1 ou 1 (ex.: /Carrinho/Alterar/7?mudanca=1)
        public IActionResult Alterar(int id, int mudanca)
        {
            Dictionary<int, int> carrinho = LerCarrinho(Request.Cookies["carrinho"]);  // lê o cookie
            if (carrinho.ContainsKey(id))                   // o produto está no carrinho?
            {
                carrinho[id] = carrinho[id] + mudanca;      // aumenta ou diminui
                if (carrinho[id] < 1)                       // chegou a zero?
                {
                    carrinho.Remove(id);                    // tira do carrinho
                }
            }

            SalvarCarrinho(carrinho);                       // grava no cookie
            return RedirectToAction("Index");               // recarrega a tela do carrinho
        }

        // Link "Remover" do carrinho
        public IActionResult Remover(int id)
        {
            Dictionary<int, int> carrinho = LerCarrinho(Request.Cookies["carrinho"]);  // lê o cookie
            carrinho.Remove(id);                            // tira o produto
            SalvarCarrinho(carrinho);                       // grava no cookie
            return RedirectToAction("Index");               // recarrega a tela do carrinho
        }

        // MOTIVO deste método: o carrinho e as etapas do pedido precisam ler o cookie; assim o código não se repete.
        // static = o PedidoController também consegue usar. "texto" é o conteúdo do cookie.
        // Transforma o texto do cookie em uma lista de id -> quantidade.
        public static Dictionary<int, int> LerCarrinho(string? texto)
        {
            Dictionary<int, int> carrinho = new Dictionary<int, int>();  // começa vazio
            if (string.IsNullOrEmpty(texto))                             // não tem cookie ainda
            {
                return carrinho;                                         // devolve vazio
            }

            foreach (string item in texto.Split(','))                    // separa os produtos pela vírgula
            {
                string[] partes = item.Split(':');                       // separa id e quantidade pelos dois-pontos
                // TryParse não quebra o site se o cookie tiver sido alterado
                if (partes.Length == 2 && int.TryParse(partes[0], out int id) && int.TryParse(partes[1], out int quantidade))
                {
                    carrinho[id] = quantidade;                           // guarda na lista
                }
            }

            return carrinho;
        }

        // MOTIVO deste método: as 3 actions que mudam o carrinho precisam gravar; assim o código não se repete.
        // Transforma a lista em texto e grava no cookie.
        private void SalvarCarrinho(Dictionary<int, int> carrinho)
        {
            List<string> itens = new List<string>();  // lista de textos, um por produto
            foreach (var item in carrinho)            // olha cada produto
            {
                itens.Add(item.Key + ":" + item.Value);  // monta "id:quantidade"
            }

            // Expires = validade de 1 ano: o carrinho continua lá ao fechar o navegador (igual Amazon)
            CookieOptions opcoes = new CookieOptions { Expires = DateTimeOffset.Now.AddYears(1) };
            Response.Cookies.Append("carrinho", string.Join(",", itens), opcoes);  // grava no navegador
        }
    }
}
