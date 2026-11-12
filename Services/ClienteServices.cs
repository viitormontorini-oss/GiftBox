using Business;
using Repository.Repositories;
using Services.Models;
using Services.Validation;
using System.Net.Mail;

namespace Services;

public class ClienteServices : GenericServices<Cliente>
{
    private readonly GenericRepository<Cliente> _clientes;
    public IValidationDictionary Validation { get; } = new ValidationDictionary();
    public ClienteServices(GenericRepository<Cliente> repository) : base(repository) => _clientes = repository;

    private bool Validar(ClienteCadastro dados, int id = 0, bool editar = false)
    {
        Validation.Clear();
        var nome = dados.Nome?.Trim() ?? "";
        var email = dados.Email?.Trim() ?? "";
        var login = dados.Login?.Trim() ?? "";
        if (nome.Length is < 1 or > 100) Validation.AddError("Nome", "Informe o nome do cliente (até 100 caracteres).");
        if (email.Length > 100 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            Validation.AddError("Email", "Informe um e-mail válido de até 100 caracteres.");
        //if (login.Length is < 1 or > 100) Validation.AddError("Login", "Informe o login (até 100 caracteres).");
        //if ((!editar && string.IsNullOrEmpty(dados.Senha)) || dados.Senha?.Length > 100)
            //Validation.AddError("Senha", "Informe uma senha de até 100 caracteres.");
        //if (string.IsNullOrWhiteSpace(dados.Telefone) || dados.Telefone.Length > 20)
            //Validation.AddError("Telefone", "Informe um telefone de até 20 caracteres.");
        // agora o telefone é digitado também no Editar: confere o tamanho, igual ao OutroTelefone
        if (dados.Telefone?.Length > 20) Validation.AddError("Telefone", "O telefone deve ter até 20 caracteres.");
        if (dados.OutroTelefone?.Length > 20) Validation.AddError("OutroTelefone", "O telefone deve ter até 20 caracteres.");
        if (dados.InformarEndereco) EnderecoValidation.Validar(dados.Endereco, Validation, exigirCidade: true);
        if (!Validation.IsValid) return false;
        // login fixo ("renan") para todos por enquanto: a conferência de login repetido fica desligada
        //if (_clientes.FindOne(c => c.login == login && c.id != id) != null) Validation.AddError("Login", "Este login já está cadastrado.");
        if (_clientes.FindOne(c => c.email == email && c.id != id) != null) Validation.AddError("Email", "Este e-mail já está cadastrado.");
        if (!Validation.IsValid) return false;
        return true;
    }

    public bool Cadastrar(ClienteCadastro dados)
    {
        if (!Validar(dados)) return false;
        // O gestor cadastra só a FICHA do cliente (contato para os pedidos), sem conta no site.
        // As colunas login e senha são obrigatórias no banco, mas aceitam texto vazio:
        //   senha vazia ("") = "ainda não tem conta". Quando a pessoa se cadastrar no site com
        //   esse mesmo e-mail, o CadastrarSite aproveita esta ficha (não recusa o e-mail).
        // limpa o e-mail: tira os espaços e passa para minúsculas
        var email = dados.Email!.Trim().ToLower();
        // monta a ficha: login igual ao e-mail e senha vazia (sem conta)
        var cliente = new Cliente { nome = dados.Nome!.Trim(), email = email, login = email, senha = "" };
        AplicarTelefones(cliente, dados);
        if (dados.InformarEndereco) cliente.Enderecos.Add(EnderecoValidation.Criar(dados.Endereco, cliente));
        return base.Insert(cliente);
    }

    private static void AplicarTelefones(Cliente cliente, ClienteCadastro dados)
    {
        // só grava se estiver preenchido: cliente que veio do site pode não ter telefone (em branco dava erro)
        if (!string.IsNullOrWhiteSpace(dados.Telefone))
            cliente.telefones.Add(new Telefone_Cliente { numero = dados.Telefone.Trim(), cliente = cliente });
        if (!string.IsNullOrWhiteSpace(dados.OutroTelefone))
            cliente.telefones.Add(new Telefone_Cliente { numero = dados.OutroTelefone.Trim(), cliente = cliente });
    }

    public bool Editar(int id, ClienteCadastro dados)
    {
        if (!Validar(dados, id, editar: true)) return false;
        var cliente = _clientes.getById(id);
        if (cliente == null) { Validation.AddError("", "Cliente não encontrado."); return false; }
        // se a tela não mandar login, mantém o que já está no banco (não deixa ficar nulo)
        cliente.nome = dados.Nome!.Trim(); cliente.email = dados.Email!.Trim();
        if (!string.IsNullOrWhiteSpace(dados.Login)) cliente.login = dados.Login.Trim();
        if (!string.IsNullOrEmpty(dados.Senha)) cliente.senha = dados.Senha;
        // a tela só mostra 2 telefones: troca só esses. Os outros (celular de pedidos do site) continuam guardados
        var mostrados = cliente.telefones.Take(2).ToList();        // os 2 telefones que apareceram na tela
        _clientes.RemoveRelated(mostrados);                        // marca para apagar do banco
        cliente.telefones.RemoveAll(t => mostrados.Contains(t));   // tira da lista do cliente
        AplicarTelefones(cliente, dados);                          // coloca os números digitados
        // Os endereços usados por pedidos são históricos e não são alterados aqui.
        var principal = cliente.Enderecos.FirstOrDefault(e => e.pedido == null);
        if (!dados.InformarEndereco && principal != null)
        {
            _clientes.RemoveRelated(new[] { principal }); cliente.Enderecos.Remove(principal);
        }
        else if (dados.InformarEndereco)
        {
            if (principal == null) cliente.Enderecos.Add(EnderecoValidation.Criar(dados.Endereco, cliente));
            else
            {
                principal.logradouro = dados.Endereco.Logradouro!.Trim(); principal.numero = dados.Endereco.Numero!.Trim(); principal.bairro = dados.Endereco.Bairro!.Trim();
                principal.cidade = dados.Endereco.Cidade!.Trim(); principal.cep = dados.Endereco.Cep!.Trim();
                principal.complemento = dados.Endereco.Complemento?.Trim();
            }
        }
        _clientes.Persist(cliente);
        return true;
    }

    public bool Excluir(int id)
    {
        Validation.Clear();
        var cliente = _clientes.getById(id);
        if (cliente == null) { Validation.AddError("", "Cliente não encontrado."); return false; }
        if (_clientes.Any(c => c.id == id && c.pedidos.Any()))
        { Validation.AddError("", "Este cliente possui pedidos e não pode ser excluído."); return false; }
        _clientes.RemoveRelated(cliente.telefones);
        _clientes.RemoveRelated(cliente.Enderecos);
        return base.Delete(cliente);
    }

    // ===================== SITE (a própria pessoa se cadastra / entra) =====================

    // Cadastro pelo site: nome, e-mail e senha (a ficha CadastroSite).
    // Diferente do Cadastrar() do gestor: aqui a senha é a que a pessoa digitou (embaralhada) e não tem telefone.
    public bool CadastrarSite(CadastroSite dados)
    {
        Validation.Clear();
        var email = dados.Email!.Trim().ToLower();   // limpa o e-mail antes de comparar e gravar

        // procura um cliente com esse e-mail
        var existente = _clientes.FindOne(c => c.email == email);
        // já existe alguém com esse e-mail
        if (existente != null)
        {
            // ficha criada pelo gestor, sem conta: a pessoa assume a ficha (continua com o histórico de pedidos)
            if (SemConta(existente))
            {
                // guarda o nome que a pessoa digitou
                existente.nome = dados.Nome!.Trim();
                // login igual ao e-mail
                existente.login = email;
                // guarda a senha embaralhada (nunca a senha pura)
                existente.senha = BCrypt.Net.BCrypt.HashPassword(dados.Senha);
                // grava as mudanças no banco
                _clientes.Persist(existente);
                // deu certo
                return true;
            }

            // esse e-mail já tem conta: anota o aviso para a tela
            Validation.AddError("Email", "Esse e-mail já está cadastrado.");
            return false;
        }

        var cliente = new Cliente
        {
            nome = dados.Nome!.Trim(),
            email = email,
            login = email,   // a coluna login é obrigatória no banco; no site a pessoa entra pelo e-mail
            senha = BCrypt.Net.BCrypt.HashPassword(dados.Senha)
        };
        return base.Insert(cliente);
    }

    // Login pelo site: devolve o cliente se o e-mail existe e a senha bate; senão anota o erro e devolve null.
    public Cliente? Autenticar(LoginSite dados)
    {
        Validation.Clear();
        var email = dados.Email!.Trim().ToLower();
        var cliente = _clientes.FindOne(c => c.email == email);

        if (cliente == null || !SenhaConfere(dados.Senha, cliente.senha))
        {
            // mesma mensagem para e-mail ou senha errados: não revela quem está cadastrado
            Validation.AddError("", "E-mail ou senha incorretos.");
            return null;
        }
        return cliente;
    }

    // Os clientes antigos (cadastrados antes do hash) têm a senha em texto puro;
    // o BCrypt dá erro ao ler isso, então conta como senha errada em vez de quebrar o site.
    // MOTIVO deste método: a regra de trocar a senha fica no service, como o professor ensina.
    // Só vale para cliente com conta. Funcionário não troca a senha por aqui.
    public bool TrocarSenha(string email, string? novaSenha, string? confirmarSenha)
    {
        Validation.Clear();  // começa sem erros
        if (string.IsNullOrEmpty(novaSenha) || novaSenha.Length < 8)  // mesma regra do cadastro
        {
            Validation.AddError("NovaSenha", "A senha precisa ter pelo menos 8 caracteres.");
            return false;
        }

        if (novaSenha != confirmarSenha)  // as duas senhas são iguais?
        {
            Validation.AddError("ConfirmarSenha", "As senhas não são iguais.");
            return false;
        }

        var cliente = _clientes.FindOne(c => c.email == email);  // procura o cliente pelo e-mail
        if (cliente == null || SemConta(cliente))                // não existe, ou é só uma ficha do gestor
        {
            // mesma mensagem nos dois casos: não revela quais e-mails têm conta
            Validation.AddError("", "Não foi possível trocar a senha. Peça o link de novo.");
            return false;
        }

        cliente.senha = BCrypt.Net.BCrypt.HashPassword(novaSenha);  // guarda a senha embaralhada
        _clientes.Persist(cliente);                                 // grava no banco
        return true;                                                // deu certo
    }

    // Cliente sem conta no site: senha vazia (jeito novo) ou o valor de enchimento antigo do gestor
    // (login "renan" + senha "1515"). Quem se cadastra pelo site nunca fica com nenhum dos dois.
    private static bool SemConta(Cliente cliente)
    {
        // || quer dizer OU: senha vazia OU o enchimento antigo
        return string.IsNullOrEmpty(cliente.senha) || (cliente.login == "renan" && cliente.senha == "1515");
    }

    private static bool SenhaConfere(string? digitada, string guardada)
    {
        if (string.IsNullOrEmpty(guardada)) return false;   // ficha sem conta: não entra

        try { return BCrypt.Net.BCrypt.Verify(digitada, guardada); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}
