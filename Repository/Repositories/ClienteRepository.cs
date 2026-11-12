using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories;

public class ClienteRepository : GenericRepository<Cliente>
{
    private readonly Context _context;
    public ClienteRepository(Context context) : base(context) => _context = context;
    // Enderecos: a lista do gestor mostra quantos endereços o cliente tem
    public override List<Cliente> getAll() => _context.Clientes.Include(c => c.telefones).Include(c => c.Enderecos).OrderBy(c => c.nome).ToList();
    public override Cliente? getById(int id) => _context.Clientes
        .Include(c => c.telefones).Include(c => c.Enderecos).ThenInclude(e => e.pedido)
        .Include(c => c.pedidos).FirstOrDefault(c => c.id == id);
}
