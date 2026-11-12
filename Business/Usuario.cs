namespace Business;

public class Usuario
{
    public int id { get; set; }
    public string nome { get; set; } = "";
    public string email { get; set; } = "";
    public string login { get; set; } = "";
    public string senha { get; set; } = "";
    public string nivel { get; set; } = "";
    public virtual List<Pedido> pedidos { get; set; } = new();
}
