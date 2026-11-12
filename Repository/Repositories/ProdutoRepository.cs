using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories
{
    public class ProdutoRepository: GenericRepository<Produto>
    {   
        private readonly Context _context;  // guardado para usar o Include (igual ao ClienteRepository)
        public ProdutoRepository(Context context) :base(context) { _context = context; }
        // Include: traz junto as fotos extras (a página do produto e as telas do gestor mostram)
        public override Produto? getById(int id) => _context.Produtos.Include(p => p.imagens).FirstOrDefault(p => p.id == id);
    }  
}