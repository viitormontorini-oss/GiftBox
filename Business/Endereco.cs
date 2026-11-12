namespace Business;

public class Endereco
{
    public int id { get; set; }
  public string logradouro { get; set; }
  public string? numero { get; set; }  // número da casa, separado da rua (igual ao diagrama de classes)
    public string bairro { get; set; }
    public string cidade { get; set; }
    public string? complemento { get; set; }
    public string cep { get; set; }
    
    public virtual Cliente Cliente { get; set; }
    public virtual Pedido? pedido { get; set; }
}
