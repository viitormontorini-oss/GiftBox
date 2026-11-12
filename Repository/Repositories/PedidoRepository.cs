using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories;

public class PedidoRepository : GenericRepository<Pedido>
{
    private readonly Context _context;
    public PedidoRepository(Context context) : base(context) => _context = context;
    public override List<Pedido> getAll() => _context.Pedidos
        .Include(p => p.cliente).Include(p => p.endereco)
        .Include(p => p.produtos).ThenInclude(i => i.produto)
        .OrderByDescending(p => p.data_pedido).ToList();
    // cliente.telefones: a tela de exibir pedido mostra o telefone do cliente
    public override Pedido? getById(int id) => _context.Pedidos
        .Include(p => p.cliente).ThenInclude(c => c.telefones).Include(p => p.usuario).Include(p => p.endereco)
        .Include(p => p.produtos).ThenInclude(i => i.produto).FirstOrDefault(p => p.id == id);
}
