using Business;
using Repository.EF;

namespace Repository.Repositories;

public class CidadeEntregaRepository(Context context) : GenericRepository<CidadeEntrega>(context);
