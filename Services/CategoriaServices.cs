using Business;
using Repository.Repositories;

namespace Services;

// Service da categoria: herda tudo do GenericServices (GetAll, GetById, Insert...)
public class CategoriaServices : GenericServices<Categoria>
{
    // recebe o repositório e passa para o GenericServices
    public CategoriaServices(GenericRepository<Categoria> repository) : base(repository) { }
}
