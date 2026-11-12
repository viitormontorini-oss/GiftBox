namespace Business;

public class Produto_Pedido
{
    public int id { get; set; }
    public int quantidade  { get; set; }
    public decimal? preco_unitario { get; set; }
    public string DescricaoCoresDecoracao  { get; set; }
    public virtual Pedido pedido { get; set; }
    public virtual Produto produto { get; set; }
}
