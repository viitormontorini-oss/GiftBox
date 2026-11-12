namespace Business;

public class Produto
{
    public int id { get; set; }
    public string nome { get; set; } = "";
    public string descricao { get; set; } = "";
    public string? imagem { get; set; }
    public double preco { get; set; }
    public virtual int quantidade { get; set; }
    public virtual List<Produto_Pedido> produtoPedidos { get; set; } = new();
    public int? categoriaId { get; set; }                  // número da categoria deste produto (o ? aceita produto sem categoria)
    public virtual Categoria? categoria { get; set; }      // a categoria deste produto
    public virtual List<ProdutoImagem> imagens { get; set; } = new();  // fotos extras (até 3), tabela ProdutoImagens
}
