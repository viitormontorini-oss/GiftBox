using Business;
using Repository.Repositories;
using Services.Validation;

namespace Services;

public class CidadeEntregaServices : GenericServices<CidadeEntrega>
{
    private readonly GenericRepository<CidadeEntrega> _cidades;
    public IValidationDictionary Validation { get; } = new ValidationDictionary();
    public CidadeEntregaServices(GenericRepository<CidadeEntrega> repository) : base(repository) => _cidades = repository;

    private bool Validar(CidadeEntrega cidade)
    {
        Validation.Clear();
        cidade.nome = cidade.nome?.Trim() ?? "";
        if (cidade.nome.Length is < 1 or > 100) Validation.AddError("nome", "Informe uma cidade de até 100 caracteres.");
        if (cidade.taxa < 0 || cidade.taxa > 9999999999.99m || decimal.Round(cidade.taxa, 2) != cidade.taxa)
            Validation.AddError("taxa", "Informe uma taxa não negativa com até duas casas decimais.");
        if (!Validation.IsValid) return false;
        if (_cidades.FindOne(c => c.nome == cidade.nome && c.id != cidade.id) != null)
            Validation.AddError("nome", "Esta cidade já está cadastrada.");
        return Validation.IsValid;
    }

    public override bool Insert(CidadeEntrega cidade) => Validar(cidade) && base.Insert(cidade);
    public override bool Update(CidadeEntrega cidade) => Validar(cidade) && base.Update(cidade);
}
