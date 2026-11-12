using Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Repository.Repositories;

namespace Services
{
    public class Produto_PedidoServices: GenericServices<Produto_Pedido>
    {
        public Produto_PedidoServices(GenericRepository<Produto_Pedido> repository) : base(repository) { }
    }
}