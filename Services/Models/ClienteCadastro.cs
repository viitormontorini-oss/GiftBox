namespace Services.Models;

public class ClienteCadastro
{
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Login { get; set; }
    public string? Senha { get; set; }
    public string? Telefone { get; set; }
    public string? OutroTelefone { get; set; }
    public bool InformarEndereco { get; set; }
    public EnderecoCadastro Endereco { get; set; } = new();
}
