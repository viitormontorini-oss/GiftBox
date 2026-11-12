namespace Business;

// MOTIVO desta classe: um produto tem várias fotos (1 produto -> várias fotos), e o Produto só tem
// espaço para uma (imagem). Esta tabela é a lista de "qual foto é de qual produto".
// A foto principal continua em Produto.imagem; aqui ficam as fotos extras (até 3).
// O arquivo da foto fica na pasta do site; o banco guarda só o caminho.
public class ProdutoImagem
{
    public int id { get; set; }                      // número da foto (o banco gera sozinho)
    public string arquivo { get; set; } = "";        // caminho da foto, ex.: /uploads/produtos/abc.jpg
    public int produtoId { get; set; }               // número do produto dono da foto
    public virtual Produto? produto { get; set; }    // o produto dono da foto
}
