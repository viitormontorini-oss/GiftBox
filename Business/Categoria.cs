namespace Business;

// Categoria da loja (Natal, Namorados...). Mesmo molde da classe Categoria das aulas do professor.
public class Categoria
{
    public int id { get; set; }                                   // número da categoria
    public string nome { get; set; } = "";                        // nome que aparece no site
    public string? imagem { get; set; }                           // caminho da foto da categoria
    public virtual List<Produto> produtos { get; set; } = new();  // os produtos desta categoria
}
