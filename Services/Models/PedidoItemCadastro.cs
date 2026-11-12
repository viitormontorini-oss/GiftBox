namespace Services.Models;

public class PedidoItemCadastro
{
    public int? ItemId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; } = 1;
    public string? Decoracao { get; set; }
}
