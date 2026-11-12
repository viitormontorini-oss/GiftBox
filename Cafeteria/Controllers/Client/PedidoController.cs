using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repository.EF;                 // onde está o Context
using Repository.Repositories;       // onde estão os repositórios
using Services;                      // onde está o PedidoServices
using Services.Models;               // onde está o PedidoCadastro
using System.Globalization;          // formato brasileiro de dinheiro
using System.Security.Claims;        // para saber quem está logado
using System.Text.Json;              // transforma o pedido em texto para caber no cookie

namespace Cafeteria.Controllers
{
    // Etapas do pedido: 1 Personalizar, 2 Entrega e pagamento, 3 Revisar.
    // O que a pessoa escolhe fica em um cookie chamado "pedido", do mesmo jeito do carrinho.
    // No final, o pedido é salvo pelo MESMO PedidoServices que o gestor usa.
    [Authorize(AuthenticationSchemes = "Cliente")]  // só entra quem fez login como cliente
    [AutoValidateAntiforgeryToken]                  // todo POST precisa do token do formulário
    public class PedidoController : Controller
    {
        private readonly PedidoServices _pedidos;            // salva o pedido
        private readonly ProdutoRepository _produtos;        // busca as cestas do carrinho
        private readonly CidadeEntregaRepository _cidades;   // busca as cidades e os fretes

        public PedidoController(Context context)
        {
            _produtos = new ProdutoRepository(context);      // cria o repositório das cestas
            _cidades = new CidadeEntregaRepository(context); // cria o repositório das cidades
            // cria o service do pedido com tudo que ele precisa (igual ao AdminPedidosController)
            _pedidos = new PedidoServices(new PedidoRepository(context), new ClienteRepository(context), _produtos, new UsuarioRepository(context), _cidades);
        }

        // ETAPA 1: abre a tela de personalizar
        public IActionResult Personalizar()
        {
            if (string.IsNullOrEmpty(Request.Cookies["carrinho"]))  // carrinho vazio?
            {
                return RedirectToAction("Index", "Carrinho");       // volta para o carrinho
            }

            PedidoCadastro dados = LerPedido();  // pega o que já foi escolhido
            PrepararTela(dados);                 // monta a lista de cestas e os valores
            return View(dados);                  // abre a tela
        }

        // ETAPA 1: recebe a mensagem do cartão e a observação
        [HttpPost]
        public IActionResult Personalizar(string? cartaoTexto, string? observacao)
        {
            PedidoCadastro dados = LerPedido();  // pega o que já foi escolhido
            dados.CartaoTexto = cartaoTexto;     // guarda a mensagem do cartão
            dados.Observacao = observacao;       // guarda a observação
            SalvarPedido(dados);                 // grava no cookie
            return RedirectToAction("EnderecoPagamento");  // vai para a etapa 2
        }

        // ETAPA 2: abre a tela de entrega e pagamento
        public IActionResult EnderecoPagamento()
        {
            if (string.IsNullOrEmpty(Request.Cookies["carrinho"]))  // carrinho vazio?
            {
                return RedirectToAction("Index", "Carrinho");       // volta para o carrinho
            }

            PedidoCadastro dados = LerPedido();  // pega o que já foi escolhido
            PrepararTela(dados);                 // monta a lista de cestas, cidades e valores
            return View(dados);                  // abre a tela
        }

        // ETAPA 2: recebe o formulário. continuar só vem preenchido quando aperta o botão "Continuar"
        [HttpPost]
        public IActionResult EnderecoPagamento(PedidoCadastro dados, string? continuar)
        {
            PedidoCadastro anterior = LerPedido();      // o que já estava guardado
            dados.CartaoTexto = anterior.CartaoTexto;   // mantém a mensagem da etapa 1
            dados.Observacao = anterior.Observacao;     // mantém a observação da etapa 1

            if (dados.FormaRecebimento == "Retirada")   // retirada não mostra cidade e endereço na tela
            {
                dados.CidadeEntregaId = anterior.CidadeEntregaId;  // mantém, se a pessoa voltar para entrega
                dados.Endereco = anterior.Endereco;                // mantém, se a pessoa voltar para entrega
            }

            SalvarPedido(dados);                        // grava no cookie

            if (continuar == null)                      // só trocou uma opção (entrega, retirada ou cidade)
            {
                return RedirectToAction("EnderecoPagamento");  // recarrega a tela com o bloco e o frete certos
            }

            if (_pedidos.ValidarEntregaSite(dados))     // preencheu tudo certo?
            {
                return RedirectToAction("Revisar");     // vai para a etapa 3
            }

            CopiarValidacoes();                         // leva os erros para a tela
            PrepararTela(dados);                        // monta a lista de cestas e os valores
            return View(dados);                         // mostra a tela de novo, com os erros
        }

        // ETAPA 3: abre a tela de revisar
        public IActionResult Revisar()
        {
            if (string.IsNullOrEmpty(Request.Cookies["carrinho"]))  // carrinho vazio?
            {
                return RedirectToAction("Index", "Carrinho");       // volta para o carrinho
            }

            PedidoCadastro dados = LerPedido();          // pega o que já foi escolhido
            if (!_pedidos.ValidarEntregaSite(dados))     // pulou a etapa 2?
            {
                return RedirectToAction("EnderecoPagamento");  // volta para a etapa 2
            }

            PrepararTela(dados);                         // monta a lista de cestas e os valores
            return View(dados);                          // abre a tela
        }

        // Botão "Confirmar pedido": salva no banco e o pedido aparece no gestor
        [HttpPost]
        public IActionResult Confirmar()
        {
            PedidoCadastro dados = LerPedido();  // pega o que foi escolhido nas etapas
            // lê o carrinho
            Dictionary<int, int> carrinho = CarrinhoController.LerCarrinho(Request.Cookies["carrinho"]);

            // o número do cliente vem do login, nunca do cookie
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int clienteId);
            dados.ClienteId = clienteId;
            dados.StatusPedido = "Em preparo";   // todo pedido novo começa assim

            dados.Itens = new List<PedidoItemCadastro>();     // lista vazia dos itens do pedido
            foreach (var item in carrinho)                    // olha cada cesta do carrinho
            {
                // item.Key = id da cesta; item.Value = quantidade. O preço o service busca no banco
                dados.Itens.Add(new PedidoItemCadastro { ProdutoId = item.Key, Quantidade = item.Value });
            }

            // null = sem funcionário: o próprio cliente fez o pedido (0..1 no diagrama de classes)
            if (_pedidos.Cadastrar(dados, null))              // salva: é o mesmo método do gestor
            {
                // pega o número do pedido que acabou de ser salvo
                int numero = _pedidos.GetAll().Where(p => p.cliente.id == clienteId).Max(p => p.id);
                Response.Cookies.Delete("carrinho");          // esvazia o carrinho
                Response.Cookies.Delete("pedido");            // apaga as escolhas
                return RedirectToAction("FinalizarCompra", new { id = numero });  // abre a tela de sucesso
            }

            CopiarValidacoes();                               // leva os erros para a tela
            PrepararTela(dados);                              // monta a lista de cestas e os valores
            return View("Revisar", dados);                    // mostra a etapa 3 de novo, com os erros
        }

        // Tela de sucesso. O id é o número do pedido
        public IActionResult FinalizarCompra(int id)
        {
            ViewBag.Numero = id.ToString("D6");  // D6 = 6 dígitos (ex.: 000005)
            return View();                       // abre a tela
        }

        // MOTIVO deste método: as 3 telas mostram a mesma lista de cestas, frete e total.
        // Prepara tudo o que elas mostram.
        private void PrepararTela(PedidoCadastro dados)
        {
            // lê o carrinho
            Dictionary<int, int> carrinho = CarrinhoController.LerCarrinho(Request.Cookies["carrinho"]);
            List<Business.Produto> cestas = new List<Business.Produto>();  // cestas que vão aparecer
            double totalProdutos = 0;                                      // soma das cestas

            foreach (var item in carrinho)                                 // olha cada cesta do carrinho
            {
                Business.Produto? cesta = _produtos.getById(item.Key);     // busca a cesta no banco
                if (cesta != null)                                         // ela ainda existe?
                {
                    cestas.Add(cesta);                                     // coloca na lista
                    totalProdutos = totalProdutos + cesta.preco * item.Value;  // preço x quantidade
                }
            }

            CultureInfo brasil = new CultureInfo("pt-BR");                 // formato brasileiro de dinheiro
            // as cidades vêm do banco: cidade nova do gestor aparece aqui sozinha
            List<Business.CidadeEntrega> cidades = _cidades.getAll().Where(c => c.ativa).OrderBy(c => c.nome).ToList();

            decimal frete = 0;                                             // retirada não paga frete
            string freteTexto = "Grátis";                                  // o que aparece na tela
            string cidadeNome = "";                                        // nome da cidade escolhida

            if (dados.FormaRecebimento == "Entrega")                       // pediu entrega?
            {
                freteTexto = "Escolha a cidade";                           // ainda não escolheu
                Business.CidadeEntrega? cidade = cidades.FirstOrDefault(c => c.id == dados.CidadeEntregaId);  // procura a cidade
                if (cidade != null)                                        // achou?
                {
                    frete = cidade.taxa;                                   // frete fixo dessa cidade
                    freteTexto = frete.ToString("C", brasil);              // escreve em reais
                    cidadeNome = cidade.nome;                              // guarda o nome
                }
            }

            ViewBag.Cestas = cestas;                                       // lista de cestas
            ViewBag.Quantidades = carrinho;                                // quantidade de cada cesta
            ViewBag.Cidades = cidades;                                     // cidades da etapa 2
            ViewBag.CidadeNome = cidadeNome;                               // cidade da etapa 3
            ViewBag.ProdutosTexto = totalProdutos.ToString("C", brasil);   // soma das cestas, em reais
            ViewBag.FreteTexto = freteTexto;                               // frete, em reais ou "Grátis"
            ViewBag.TotalTexto = ((decimal)totalProdutos + frete).ToString("C", brasil);  // total, em reais
        }

        // MOTIVO deste método: as etapas leem o cookie várias vezes; assim o código não se repete.
        // Lê as escolhas guardadas no cookie "pedido".
        private PedidoCadastro LerPedido()
        {
            string? texto = Request.Cookies["pedido"];   // lê o cookie
            if (!string.IsNullOrEmpty(texto))            // já tem alguma escolha guardada?
            {
                try
                {
                    // transforma o texto de volta em PedidoCadastro
                    PedidoCadastro? dados = JsonSerializer.Deserialize<PedidoCadastro>(texto);
                    if (dados != null)
                    {
                        return dados;                    // devolve as escolhas
                    }
                }
                catch (JsonException)                    // cookie estragado
                {
                    // não faz nada: continua e começa do zero
                }
            }

            // primeira vez: começa com entrega e Pix marcados, como no desenho da tela
            return new PedidoCadastro { FormaRecebimento = "Entrega", TipoPagamento = "Pix" };
        }

        // MOTIVO deste método: as etapas 1 e 2 gravam no cookie; assim o código não se repete.
        // Sem Expires: o cookie some quando fechar o navegador. HttpOnly: o endereço não fica visível para scripts.
        private void SalvarPedido(PedidoCadastro dados)
        {
            string texto = JsonSerializer.Serialize(dados);                // transforma o pedido em texto
            CookieOptions opcoes = new CookieOptions { HttpOnly = true };  // opções do cookie
            Response.Cookies.Append("pedido", texto, opcoes);              // grava no navegador
        }

        // MOTIVO deste método: as etapas 2 e 3 mostram erros do service.
        // Copia os erros do PedidoServices para a tela (igual ao ContaController).
        private void CopiarValidacoes()
        {
            foreach (var erro in _pedidos.Validation.Errors)   // cada campo com erro
            {
                foreach (var mensagem in erro.Value)           // cada mensagem desse campo
                {
                    ModelState.AddModelError(erro.Key, mensagem);  // entrega para a tela
                }
            }
        }
    }
}
