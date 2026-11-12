namespace Services.Models;

public class EnderecoCadastro
{
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }  // número da casa
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Cep { get; set; }
    public string? Complemento { get; set; }
}
