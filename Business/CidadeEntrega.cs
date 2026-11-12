namespace Business;

public class CidadeEntrega
{
    public int id { get; set; }
    public string nome { get; set; } = "";
    public decimal taxa { get; set; }
    public bool ativa { get; set; } = true;
}
