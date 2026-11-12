using Business;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Repository.EF;
using Repository.Repositories;
using Services;
using Services.Models;
using Services.Validation;
using System.Security.Claims;


namespace Cafeteria.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class ContaController : Controller
    {
        // garçom (controller) -> atendente (ClienteServices) -> arquivista (ClienteRepository) -> banco
        private readonly ClienteServices _clientes;   // regras do cliente
                                                      // significa que o campo só pode receber valor na declaração ou no construtor
        private readonly UsuarioServices _usuarios;   // regras do funcionário (tabela usuarios)
                                                      // significa que o campo só pode receber valor na declaração ou no construtor

        public ContaController(Context context)
        {
            _clientes = new ClienteServices(new ClienteRepository(context));
            _usuarios = new UsuarioServices(new UsuarioRepository(context));
        }


        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginSite dados)
        {
            // LOGIN ÚNICO (funcionário e cliente na mesma tela)
            // 1. confere se e-mail e senha foram preenchidos corretamente (ModelState)
            // 2. é FUNCIONÁRIO? (tabela usuarios) -> crachá de funcionário -> painel do gestor
            // 3. é CLIENTE? (o ClienteServices confere e-mail e senha) -> carteirinha de cliente -> página inicial
            // 4. nenhum dos dois: "E-mail ou senha incorretos." e mostra a tela de novo

            if (ModelState.IsValid)
            {
                dados.Email = dados.Email!.Trim().ToLower();

                // 1º: é FUNCIONÁRIO? quem confere é o UsuarioServices (tabela usuarios)
                Usuario? usuario = _usuarios.AutenticarPorEmail(dados.Email, dados.Senha);
                if (usuario != null)
                {
                    // crachá de funcionário: número, nome e CARGO (Administrador ou Gestor)
                    var identidade = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, usuario.id.ToString()),
                        new Claim(ClaimTypes.Name, usuario.nome),
                        new Claim(ClaimTypes.Role, usuario.nivel)
                    }, CookieAuthenticationDefaults.AuthenticationScheme);

                    // entrega o crachá de FUNCIONÁRIO (cookie do gestor, o 1º do Program.cs)
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade));

                    // vai para o painel do gestor
                    return RedirectToAction("Index", "AdminPrincipal", new { area = "Admin" });
                }

                // 2º: é CLIENTE? quem confere é o atendente (ClienteServices.Autenticar)
                Cliente? cliente = _clientes.Autenticar(dados);
                if (cliente != null)
                {
                    // carteirinha de cliente: número, nome e tipo "Cliente"
                    var identidade = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, cliente.id.ToString()),
                        new Claim(ClaimTypes.Name, cliente.nome),
                        new Claim(ClaimTypes.Role, "Cliente")
                    }, "Cliente");

                    await HttpContext.SignInAsync("Cliente", new ClaimsPrincipal(identidade));

                    // tem cesta no carrinho? a pessoa estava comprando: volta para o carrinho
                    if (!string.IsNullOrEmpty(Request.Cookies["carrinho"]))
                    {
                        return RedirectToAction("Index", "Carrinho");
                    }

                    return RedirectToAction("Index", "Home");
                }

                // se chegou aqui, o e-mail ou a senha não bateram: copia o aviso do atendente para a tela
                CopiarValidacoes(_clientes.Validation);
            }

            return View(dados);
        }




        public IActionResult Cadastro()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Cadastro(CadastroSite dados)
        {
            if (ModelState.IsValid)
            {
                // e-mail de funcionário não pode virar conta de cliente
                // (o login único procura primeiro nos funcionários: o cliente nunca conseguiria entrar)
                if (_usuarios.ExisteEmail(dados.Email!.Trim().ToLower()))
                {
                    ModelState.AddModelError("Email", "Esse e-mail já está cadastrado.");
                    return View(dados);
                }

                // o atendente (ClienteServices) confere o e-mail repetido, embaralha a senha e grava
                if (_clientes.CadastrarSite(dados))
                    return RedirectToAction("Login");

                // não deu (ex.: e-mail repetido): copia o aviso do atendente para a tela
                CopiarValidacoes(_clientes.Validation);
            }
            return View(dados);
        }

        //sair do login

        [HttpPost]
        public async Task<IActionResult> Sair()
        {
            await HttpContext.SignOutAsync("Cliente");
            return RedirectToAction("Index", "Home");
        }


        public IActionResult EsqueciSenha()
        {
            return View();
        }
        // Botão "Enviar link". Como o site não envia e-mail, o e-mail fica guardado em um cookie por 30 minutos
        // e o botão da tela "Confira seu e-mail" abre a nova senha em outra guia, fazendo o papel do link do e-mail.
        // ATENÇÃO: não é o jeito seguro. O certo é mandar um link de verdade por e-mail.
        [HttpPost]
        public IActionResult EsqueciSenha(string email)
        {
            string limpo = (email ?? "").Trim().ToLower();                  // tira os espaços e passa para minúsculas
            // HttpOnly: scripts da página não leem o cookie; Expires: some em 30 minutos
            CookieOptions opcoes = new CookieOptions { HttpOnly = true, Expires = DateTimeOffset.Now.AddMinutes(30) };
            Response.Cookies.Append("trocarSenhaEmail", limpo, opcoes);     // guarda o e-mail no navegador
            return RedirectToAction("LinkEnviado");                         // mostra "Confira seu e-mail"
        }

        public IActionResult LinkEnviado()
        {
            return View();
        }
        public IActionResult TrocarSenha()
        {
            return View();
        }

        // Botão "Salvar nova senha"
        [HttpPost]
        public IActionResult TrocarSenha(string? novaSenha, string? confirmarSenha)
        {
            string? email = Request.Cookies["trocarSenhaEmail"];  // pega o e-mail guardado em "Enviar link"
            if (string.IsNullOrEmpty(email))                      // o cookie venceu ou não existe
            {
                ModelState.AddModelError("", "O link expirou. Peça um novo em Esqueci a senha.");  // avisa na tela
                return View();                                    // mostra a tela de novo
            }

            if (_clientes.TrocarSenha(email, novaSenha, confirmarSenha))  // o service confere e grava
            {
                Response.Cookies.Delete("trocarSenhaEmail");      // apaga o cookie: o link só vale uma vez
                return RedirectToAction("Login");                 // volta para o login
            }

            CopiarValidacoes(_clientes.Validation);               // leva os erros para a tela
            return View();                                        // mostra a tela de novo, com os erros
        }

        // copia os erros anotados pelo service (Validation) para o caderno da tela (ModelState)
        // mesmo jeito do CopiarValidacoes do AdminBaseController do gestor
        private void CopiarValidacoes(IValidationDictionary validation)
        {
            foreach (var erro in validation.Errors)
                foreach (var mensagem in erro.Value)
                    ModelState.AddModelError(erro.Key, mensagem);
        }
    }
}
