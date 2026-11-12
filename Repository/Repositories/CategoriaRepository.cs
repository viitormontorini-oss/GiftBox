using Business;
using Repository.EF;

namespace Repository.Repositories
{
    // Repositório da categoria: herda tudo do GenericRepository (buscar, inserir, alterar, remover)
    public class CategoriaRepository : GenericRepository<Categoria>
    {
        // recebe o Context e passa para o GenericRepository
        public CategoriaRepository(Context context) : base(context) { }
    }
}
