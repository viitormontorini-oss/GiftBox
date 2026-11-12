using Business;
using Repository.Repositories;
using Services.Models;
using Services.Validation;

namespace Services;

public class PedidoServices : GenericServices<Pedido>
{
    private readonly GenericRepository<Pedido> _pedidos;
    private readonly GenericRepository<Cliente> _clientes;
    private readonly GenericRepository<Produto> _produtos;
    private readonly GenericRepository<Usuario> _usuarios;
    private readonly GenericRepository<CidadeEntrega> _cidades;
    public IValidationDictionary Validation { get; } = new ValidationDictionary();

    public PedidoServices(GenericRepository<Pedido> repository, GenericRepository<Cliente> clientes,
        GenericRepository<Produto> produtos, GenericRepository<Usuario> usuarios,
        GenericRepository<CidadeEntrega> cidades) : base(repository)
    {
        _pedidos = repository; _clientes = clientes; _produtos = produtos; _usuarios = usuarios; _cidades = cidades;
    }

    // usuarioId = null: o pedido foi feito pelo próprio cliente no site
    public bool Cadastrar(PedidoCadastro dados, int? usuarioId) => Salvar(dados, usuarioId, null);
    public bool Editar(int id, PedidoCadastro dados, int usuarioId) => Salvar(dados, usuarioId, id);

    private bool Salvar(PedidoCadastro dados, int? usuarioId, int? id)
    {
        Validation.Clear();
        var existente = id.HasValue ? _pedidos.getById(id.Value) : null;
        if (id.HasValue && existente == null) { Validation.AddError("", "Pedido não encontrado."); return false; }
        if (dados.ClienteId <= 0) Validation.AddError("ClienteId", "Selecione o cliente.");
        if (usuarioId <= 0) Validation.AddError("", "Entre novamente para cadastrar o pedido.");
        if (dados.FormaRecebimento != "Retirada" && dados.FormaRecebimento != "Entrega")
            Validation.AddError("FormaRecebimento", "Selecione entrega ou retirada.");
        if (dados.TipoPagamento != "Pix" && dados.TipoPagamento != "Dinheiro" && dados.TipoPagamento != "Cartão")
            Validation.AddError("TipoPagamento", "Selecione a forma de pagamento.");
        if (!new[] { "Em preparo", "Pronto", "Em entrega", "Entregue" }.Contains(dados.StatusPedido))
            Validation.AddError("StatusPedido", "Selecione um status válido.");
        if (dados.CartaoTexto?.Length > 500) Validation.AddError("CartaoTexto", "A mensagem deve ter até 500 caracteres.");
        if (dados.Observacao?.Length > 300) Validation.AddError("Observacao", "A observação deve ter até 300 caracteres.");
        if (dados.Itens == null || dados.Itens.Count == 0) Validation.AddError("Itens", "Adicione pelo menos um produto.");
        for (var i = 0; i < (dados.Itens?.Count ?? 0); i++)
        {
            var item = dados.Itens![i];
            if (item.ProdutoId <= 0) Validation.AddError($"Itens[{i}].ProdutoId", "Selecione o produto.");
            if (item.Quantidade <= 0) Validation.AddError($"Itens[{i}].Quantidade", "A quantidade deve ser maior que zero.");
            if (item.Decoracao?.Length > 500) Validation.AddError($"Itens[{i}].Decoracao", "A decoração deve ter até 500 caracteres.");
        }
        if (dados.FormaRecebimento == "Entrega")
        {
            if (dados.CidadeEntregaId.GetValueOrDefault() <= 0) Validation.AddError("CidadeEntregaId", "Selecione a cidade de entrega.");
            EnderecoValidation.Validar(dados.Endereco, Validation, exigirCidade: false);
        }
        if (!Validation.IsValid) return false;
        var cliente = _clientes.FindOne(c => c.id == dados.ClienteId);
        var usuario = _usuarios.FindOne(u => u.id == usuarioId);
        if (cliente == null) Validation.AddError("ClienteId", "Cliente não encontrado.");
        // só confere o funcionário quando o pedido veio do gestor
        if (usuarioId != null && usuario == null) Validation.AddError("", "Usuário não encontrado. Entre novamente.");
        CidadeEntrega? cidade = null;
        if (dados.FormaRecebimento == "Entrega")
        {
            cidade = _cidades.FindOne(c => c.id == dados.CidadeEntregaId);
            if (cidade == null || (!cidade.ativa && !(existente?.endereco != null && (existente.cidade_entrega_id == cidade.id || existente.endereco.cidade == cidade.nome)))) Validation.AddError("CidadeEntregaId", "Esta cidade não está disponível para entrega.");
        }
        var itens = new List<Produto_Pedido>();
        for (var i = 0; i < dados.Itens!.Count; i++)
        {
            var input = dados.Itens[i];
            var produto = _produtos.FindOne(p => p.id == input.ProdutoId);
            if (produto == null) { Validation.AddError($"Itens[{i}].ProdutoId", "Produto não encontrado."); continue; }
            if (!double.IsFinite(produto.preco) || produto.preco < 0 || produto.preco > 9999999999.99)
            { Validation.AddError($"Itens[{i}].ProdutoId", "O preço deste produto precisa ser corrigido no cadastro."); continue; }
            var anterior = input.ItemId.HasValue ? existente?.produtos.FirstOrDefault(i => i.id == input.ItemId) : null;
            if (input.ItemId.HasValue && anterior == null) { Validation.AddError($"Itens[{i}].ProdutoId", "Item não pertence a este pedido."); continue; }
            itens.Add(new Produto_Pedido
            {
                produto = produto, quantidade = input.Quantidade,
                DescricaoCoresDecoracao = input.Decoracao?.Trim() ?? "",
                preco_unitario = anterior != null && anterior.produto.id == produto.id ? anterior.preco_unitario : decimal.Round((decimal)produto.preco, 2, MidpointRounding.AwayFromZero)
            });
        }
        if (!Validation.IsValid) return false;
        var manterTaxa = cidade != null && existente?.endereco != null &&
            (existente.cidade_entrega_id == cidade.id || (existente.cidade_entrega_id == null && existente.endereco.cidade == cidade.nome));
        var pedido = new Pedido
        {
            cliente = cliente!, usuario = existente?.usuario ?? usuario, produtos = itens, data_pedido = existente?.data_pedido ?? DateTime.Now,
            tipo_pagamento = dados.TipoPagamento!, status_pedido = dados.StatusPedido!,
            cartao_texto = dados.CartaoTexto?.Trim() ?? "", observacao = dados.Observacao?.Trim() ?? "",
            nome_recebe = dados.NomeRecebe?.Trim(),  // nome de quem recebe (vem do site)
            taxa_entrega = manterTaxa ? existente!.taxa_entrega : cidade?.taxa ?? 0,
            cidade_entrega_id = cidade?.id,
            endereco = cidade == null ? null : EnderecoValidation.Criar(dados.Endereco, cliente!, manterTaxa ? existente!.endereco!.cidade : cidade.nome)
        };
        // site: guarda o celular do pedido na tabela de telefones do cliente, se ainda não estiver lá
        if (!string.IsNullOrWhiteSpace(dados.Celular))
        {
            var celular = dados.Celular.Trim();  // tira os espaços das pontas
            // esse cliente já tem esse número?
            if (!_clientes.Any(c => c.id == cliente!.id && c.telefones.Any(t => t.numero == celular)))
            {
                cliente!.telefones.Add(new Telefone_Cliente { numero = celular, cliente = cliente });  // acrescenta o telefone
            }
        }

        foreach (var item in itens) item.pedido = pedido;
        if (existente == null) return base.Insert(pedido);
        _pedidos.RemoveRelated(existente.produtos);
        existente.produtos.Clear();
        foreach (var item in itens) { item.pedido = existente; existente.produtos.Add(item); }
        existente.cliente = pedido.cliente;
        existente.tipo_pagamento = pedido.tipo_pagamento; existente.status_pedido = pedido.status_pedido;
        existente.cartao_texto = pedido.cartao_texto; existente.observacao = pedido.observacao;
        existente.taxa_entrega = pedido.taxa_entrega; existente.cidade_entrega_id = pedido.cidade_entrega_id;
        if (existente.endereco != null)
        {
            if (pedido.endereco == null) { _pedidos.RemoveRelated(new[] { existente.endereco }); existente.endereco = null; }
            else
            {
                existente.endereco.Cliente = pedido.cliente;
                existente.endereco.logradouro = pedido.endereco.logradouro; existente.endereco.numero = pedido.endereco.numero; existente.endereco.bairro = pedido.endereco.bairro;
                existente.endereco.cidade = pedido.endereco.cidade; existente.endereco.cep = pedido.endereco.cep;
                existente.endereco.complemento = pedido.endereco.complemento;
            }
        }
        else existente.endereco = pedido.endereco;
        _pedidos.Persist(existente);
        return true;
    }
    // MOTIVO deste método: mostrar o erro na etapa de entrega do site, em vez de só no final.
    // A regra fica no service, como o professor ensina.
    public bool ValidarEntregaSite(PedidoCadastro dados)
    {
        Validation.Clear();  // começa sem erros
        // nome de quem recebe: obrigatório, até 60 caracteres
        if (string.IsNullOrWhiteSpace(dados.NomeRecebe) || dados.NomeRecebe.Length > 60) Validation.AddError("NomeRecebe", "Informe o nome de quem vai receber.");
        // celular: obrigatório, a equipe confirma o pedido por ele
        if (string.IsNullOrWhiteSpace(dados.Celular) || dados.Celular.Length > 20) Validation.AddError("Celular", "Informe o celular para contato.");
        // forma de pagamento: só aceita as três opções
        if (dados.TipoPagamento != "Pix" && dados.TipoPagamento != "Dinheiro" && dados.TipoPagamento != "Cartão") Validation.AddError("TipoPagamento", "Selecione a forma de pagamento.");
        if (dados.FormaRecebimento == "Entrega")  // só quem pediu entrega precisa de cidade e endereço
        {
            // cidade: precisa ter escolhido uma
            if (dados.CidadeEntregaId.GetValueOrDefault() <= 0) Validation.AddError("CidadeEntregaId", "Selecione a cidade de entrega.");
            // endereço: usa a mesma regra do gestor
            EnderecoValidation.Validar(dados.Endereco, Validation, exigirCidade: false);
        }
        return Validation.IsValid;  // true = sem erros
    }

    public bool Excluir(int id)
    {
        Validation.Clear();
        var pedido = _pedidos.getById(id);
        if (pedido == null) { Validation.AddError("", "Pedido não encontrado."); return false; }
        _pedidos.RemoveRelated(pedido.produtos);
        // O endereço permanece no cadastro do cliente, sem apagar dados de contato.
        return base.Delete(pedido);
    }
}
