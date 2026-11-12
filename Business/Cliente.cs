namespace Business;

public class Cliente
{
    public int id  { get; set; }
    public string nome { get; set; }
    public string email { get; set; }
    public string login { get; set; }
    public string senha { get; set; }
    
    public virtual List<Endereco>  Enderecos { get; set; } = new();
    public virtual List<Pedido> pedidos { get; set; } = new();
    public virtual List<Telefone_Cliente>  telefones { get; set; } = new();
}
