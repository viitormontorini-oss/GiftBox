namespace Business;

public class Pedido
{
    public int id { get; set; }
    public DateTime data_pedido { get; set; }
    public string observacao { get; set; }
    public string cartao_texto { get; set; }
    public string status_pedido { get; set; }
    public string tipo_pagamento { get; set; }
    
    public virtual Cliente cliente { get; set; }
    
    public virtual List<Produto_Pedido>  produtos { get; set; }
    
    public virtual Endereco? endereco { get; set; }

    public decimal? taxa_entrega { get; set; }
    public int? cidade_entrega_id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal? Total => taxa_entrega == null || produtos.Any(item => item.preco_unitario == null)
        ? null : produtos.Sum(item => item.preco_unitario!.Value * item.quantidade) + taxa_entrega.Value;
    
    public virtual Usuario? usuario { get; set; }  // opcional (0..1 no diagrama): só existe quando um funcionário cadastra
    public string? nome_recebe { get; set; }       // nome de quem vai receber a cesta
}
