namespace Services.Models;

public class ProdutoCadastro
{
    public string? Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal? Preco { get; set; }
    public int? Quantidade { get; set; }
    public int? CategoriaId { get; set; }  // categoria escolhida no cadastro
}
