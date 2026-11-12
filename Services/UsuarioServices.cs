using Business;
using Repository.Repositories;
using Services.Validation;
using Services.Models;
using System.Net.Mail;

namespace Services;

public class UsuarioServices : GenericServices<Usuario>
{
    private readonly GenericRepository<Usuario> _usuarios;
    public IValidationDictionary Validation { get; } = new ValidationDictionary();

    public UsuarioServices(GenericRepository<Usuario> repository) : base(repository)
    {
        _usuarios = repository;
    }

    public bool GarantirAdministradorInicial(string? senhaInicial = null)
    {
        if (_usuarios.FindOne(u => true) != null) return false;

        if (string.IsNullOrWhiteSpace(senhaInicial) || senhaInicial.Length < 12)
            throw new InvalidOperationException("Configure BootstrapAdmin__Password com pelo menos 12 caracteres para criar o primeiro administrador.");
        var administrador = new Usuario
        {
            nome = "Administrador GiftBox",
            email = "admin@giftbox.local",
            login = "admin",
            senha = Embaralhar(senhaInicial),
            nivel = "Administrador"
        };
        _usuarios.Insert(administrador);
        _usuarios.Persist(administrador);
        return true;
    }

    public Usuario? Autenticar(string? login, string? senha)
    {
        Validation.Clear();
        if (string.IsNullOrWhiteSpace(login)) Validation.AddError("usuario", "Informe o login.");
        if (string.IsNullOrEmpty(senha)) Validation.AddError("senha", "Informe a senha.");
        if (!Validation.IsValid) return null;
        var usuario = _usuarios.FindOne(u => u.login == login!.Trim());
        if (usuario == null || !SenhaConfere(usuario, senha))
        {
            Validation.AddError("", "Login ou senha inválidos.");
            return null;
        }
        return usuario;
    }

    // Login único do site (/Conta/Login): o funcionário entra pelo E-MAIL, na mesma tela do cliente.
    // Não anota erro: se não for funcionário, o ContaController tenta o cliente em seguida.
    // o e-mail já pertence a um funcionário? (o cadastro do site usa para não deixar um cliente pegar esse e-mail)
    public bool ExisteEmail(string email) => _usuarios.Any(u => u.email.ToLower() == email);

    public Usuario? AutenticarPorEmail(string email, string? senha)
    {
        Validation.Clear();
        var usuario = _usuarios.FindOne(u => u.email.ToLower() == email);
        if (usuario == null || !SenhaConfere(usuario, senha)) return null;
        return usuario;
    }

    private bool Validar(Usuario entity, bool validarSenha)
    {
        Validation.Clear();
        if (entity == null) { Validation.AddError("", "Informe os dados do usuário."); return false; }
        entity.nome = entity.nome?.Trim() ?? "";
        entity.email = entity.email?.Trim() ?? "";
        entity.login = entity.login?.Trim() ?? "";
        if (entity.nome.Length is < 1 or > 100) Validation.AddError("nome", "Informe um nome de até 100 caracteres.");
        if (entity.email.Length > 100 || !MailAddress.TryCreate(entity.email, out var email) || email.Address != entity.email)
            Validation.AddError("email", "Informe um e-mail válido de até 100 caracteres.");
        if (entity.login.Length is < 1 or > 100) Validation.AddError("login", "Informe um login de até 100 caracteres.");
        if (validarSenha && (string.IsNullOrEmpty(entity.senha) || entity.senha.Length is < 6 or > 100))
            Validation.AddError("senha", "A senha deve ter entre 6 e 100 caracteres.");
        if (entity.nivel != "Administrador" && entity.nivel != "Gestor")
            Validation.AddError("nivel", "Selecione Administrador ou Gestor.");
        if (!Validation.IsValid) return false;
        if (_usuarios.FindOne(u => u.login == entity.login && u.id != entity.id) != null)
            Validation.AddError("login", "Este login já está cadastrado.");
        if (_usuarios.FindOne(u => u.email == entity.email && u.id != entity.id) != null)
            Validation.AddError("email", "Este e-mail já está cadastrado.");
        if (!Validation.IsValid) return false;
        return true;
    }
    public override bool Insert(Usuario entity)
    {
        if (!Validar(entity, true)) return false;   // confere a senha digitada (6 a 100 caracteres)
        entity.senha = Embaralhar(entity.senha);      // e só então guarda embaralhada
        return base.Insert(entity);
    }

    public bool Editar(int id, UsuarioEdicao dados)
    {
        Validation.Clear();
        var atual = _usuarios.getById(id);
        if (atual == null) { Validation.AddError("", "Usuário não encontrado."); return false; }
        var novo = new Usuario { id = id, nome = dados.Nome ?? "", email = dados.Email ?? "", login = dados.Login ?? "",
            senha = string.IsNullOrEmpty(dados.NovaSenha) ? atual.senha : dados.NovaSenha, nivel = dados.Nivel ?? "" };
        if (!Validar(novo, !string.IsNullOrEmpty(dados.NovaSenha))) return false;
        if (atual.nivel == "Administrador" && novo.nivel != "Administrador" && !_usuarios.Any(u => u.id != id && u.nivel == "Administrador"))
        { Validation.AddError("Nivel", "Mantenha pelo menos um administrador no sistema."); return false; }
        atual.nome = novo.nome; atual.email = novo.email; atual.login = novo.login; atual.nivel = novo.nivel;
        if (!string.IsNullOrEmpty(dados.NovaSenha)) atual.senha = Embaralhar(dados.NovaSenha);   // senha nova embaralhada
        _usuarios.Persist(atual);
        return true;
    }

    public bool Excluir(int id, int usuarioLogadoId)
    {
        Validation.Clear();
        var usuario = _usuarios.getById(id);
        if (usuario == null) { Validation.AddError("", "Usuário não encontrado."); return false; }
        if (id == usuarioLogadoId) { Validation.AddError("", "Você não pode excluir o usuário que está conectado."); return false; }
        if (usuario.nivel == "Administrador" && !_usuarios.Any(u => u.id != id && u.nivel == "Administrador"))
        { Validation.AddError("", "O último administrador não pode ser excluído."); return false; }
        if (_usuarios.Any(u => u.id == id && u.pedidos.Any()))
        { Validation.AddError("", "Este usuário está vinculado a pedidos e não pode ser excluído."); return false; }
        return base.Delete(usuario);
    }

    // ===================== SENHA DO FUNCIONÁRIO (BCrypt) =====================

    // embaralha a senha antes de guardar (o mesmo BCrypt da senha do cliente)
    private static string Embaralhar(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);

    // confere a senha digitada com a guardada.
    // Senhas antigas (de antes do BCrypt, ex.: "1515") ainda estão em texto puro no banco:
    // se a digitada bater, a senha é embaralhada e regravada na hora (na próxima vez já confere pelo BCrypt).
    // Assim o banco de cada pessoa do grupo se corrige sozinho no primeiro login, sem mexer no MySQL na mão.
    private bool SenhaConfere(Usuario usuario, string? digitada)
    {
        if (string.IsNullOrEmpty(digitada)) return false;

        if (usuario.senha.StartsWith("$2"))   // já está embaralhada (todo hash BCrypt começa com $2a$, $2b$...)
        {
            try { return BCrypt.Net.BCrypt.Verify(digitada, usuario.senha); }
            catch (BCrypt.Net.SaltParseException) { return false; }
        }

        if (usuario.senha != digitada) return false;   // senha antiga em texto puro, e não bateu

        usuario.senha = Embaralhar(digitada);          // bateu: troca pela versão embaralhada
        _usuarios.Persist(usuario);
        return true;
    }
}
