using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories
{
    public class ProdutoPedidoRepository: GenericRepository<Produto_Pedido>
    {   
        public ProdutoPedidoRepository(Context context) :base(context) {}
    }  
}