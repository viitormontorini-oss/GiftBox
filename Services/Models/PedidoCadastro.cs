namespace Services.Models;

public class PedidoCadastro
{
    public int ClienteId { get; set; }
    public List<PedidoItemCadastro> Itens { get; set; } = new() { new() };
    public string? FormaRecebimento { get; set; } = "Retirada";
    public int? CidadeEntregaId { get; set; }
    public EnderecoCadastro Endereco { get; set; } = new();
    public string? TipoPagamento { get; set; }
    public string? StatusPedido { get; set; } = "Em preparo";
    public string? CartaoTexto { get; set; }
    public string? Observacao { get; set; }
    public string? NomeRecebe { get; set; }  // site: nome de quem vai receber
    public string? Celular { get; set; }     // site: celular para a equipe confirmar pelo WhatsApp
}
