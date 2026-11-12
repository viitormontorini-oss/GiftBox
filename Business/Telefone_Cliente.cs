namespace Business;

public class Telefone_Cliente
{
    public int id { get; set; }
    public string numero { get; set; }
    
    public virtual Cliente cliente { get; set; }
}