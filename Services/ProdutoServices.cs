using Business;
using Repository.Repositories;
using Services.Models;
using Services.Validation;

namespace Services;

public class ProdutoServices : GenericServices<Produto>
{
    private readonly GenericRepository<Produto> _produtos;
    public IValidationDictionary Validation { get; } = new ValidationDictionary();
    public ProdutoServices(GenericRepository<Produto> repository) : base(repository) => _produtos = repository;

    public bool Validar(ProdutoCadastro dados)
    {
        Validation.Clear();
        if (string.IsNullOrWhiteSpace(dados.Nome) || dados.Nome.Length > 100) Validation.AddError("Nome", "Informe um nome de até 100 caracteres.");
        if (dados.Descricao?.Length > 5000) Validation.AddError("Descricao", "A descrição deve ter até 5000 caracteres.");
        if (dados.Preco == null || dados.Preco < 0 || dados.Preco > 9999999999.99m || decimal.Round(dados.Preco.Value, 2) != dados.Preco)
            Validation.AddError("Preco", "Informe um preço não negativo com até duas casas decimais.");
        if (dados.Quantidade == null || dados.Quantidade < 0) Validation.AddError("Quantidade", "Informe um estoque não negativo.");
        // sem categoria o produto não aparece em nenhuma página do site
        if (dados.CategoriaId == null) Validation.AddError("CategoriaId", "Escolha a categoria do produto.");
        return Validation.IsValid;
    }

    // fotos = caminhos das fotos extras já salvas na pasta (até 3)
    public bool Cadastrar(ProdutoCadastro dados, string? imagem, List<string> fotos)
    {
        if (!Validar(dados)) return false;
        if (fotos.Count > 3)  // máximo de 3 fotos extras (4 com a principal)
        {
            Validation.AddError("fotos", "Escolha até 3 fotos além da principal.");
            return false;
        }

        var produto = new Produto { nome = dados.Nome!.Trim(), descricao = dados.Descricao?.Trim() ?? "",
            preco = (double)dados.Preco!.Value, quantidade = dados.Quantidade!.Value, imagem = imagem, categoriaId = dados.CategoriaId };
        foreach (string arquivo in fotos)  // uma linha na tabela ProdutoImagens para cada foto extra
        {
            produto.imagens.Add(new ProdutoImagem { arquivo = arquivo, produto = produto });
        }
        return base.Insert(produto);
    }

    // fotos = fotos extras novas (caminhos); removerFoto = número da foto extra do botão Remover (ou nenhum)
    public bool Editar(int id, ProdutoCadastro dados, string? imagem, bool removerImagem, List<string> fotos, int? removerFoto)
    {
        if (!Validar(dados)) return false;
        var produto = _produtos.getById(id);
        if (produto == null) { Validation.AddError("", "Produto não encontrado."); return false; }

        // confere o limite antes de mudar qualquer coisa
        var remover = produto.imagens.FirstOrDefault(f => f.id == removerFoto);  // a foto do Remover (null se nenhuma)
        int quantas = produto.imagens.Count + fotos.Count;                       // fotos extras depois de salvar
        if (remover != null) quantas = quantas - 1;                              // essa sai
        if (quantas > 3)
        {
            Validation.AddError("fotos", "O produto pode ter até 3 fotos além da principal.");
            return false;
        }
        produto.nome = dados.Nome!.Trim(); produto.descricao = dados.Descricao?.Trim() ?? "";
        produto.preco = (double)dados.Preco!.Value; produto.quantidade = dados.Quantidade!.Value;
        produto.categoriaId = dados.CategoriaId;  // guarda a categoria escolhida
        if (imagem != null || removerImagem) produto.imagem = imagem;
        if (remover != null)  // o gestor clicou em Remover numa foto extra
        {
            _produtos.RemoveRelated(new[] { remover });  // marca a linha para apagar do banco
            produto.imagens.Remove(remover);             // tira da lista do produto
        }
        foreach (string arquivo in fotos)  // cada foto extra nova vira uma linha na tabela
        {
            produto.imagens.Add(new ProdutoImagem { arquivo = arquivo, produto = produto });
        }
        _produtos.Persist(produto);
        return true;
    }

    // MOTIVO deste método: o botão Inativar do gestor. Não cria campo novo no banco:
    // usa o esquema que já existia, estoque 0 = Inativo
    public bool Inativar(int id)
    {
        Validation.Clear();                                  // começa sem erros
        var produto = _produtos.getById(id);                 // procura o produto no banco
        if (produto == null)                                 // não achou
        {
            Validation.AddError("", "Produto não encontrado.");
            return false;
        }

        produto.quantidade = 0;                              // estoque 0 = Inativo (some do site)
        _produtos.Persist(produto);                          // grava no banco
        return true;                                         // deu certo
    }

    public bool Excluir(int id)
    {
        Validation.Clear();
        var produto = _produtos.getById(id);
        if (produto == null) { Validation.AddError("", "Produto não encontrado."); return false; }
        if (_produtos.Any(p => p.id == id && p.produtoPedidos.Any()))
        { Validation.AddError("", "Este produto está vinculado a pedidos e não pode ser excluído."); return false; }
        _produtos.RemoveRelated(produto.imagens);  // apaga antes as linhas das fotos extras (elas dependem do produto)
        return base.Delete(produto);
    }
}
