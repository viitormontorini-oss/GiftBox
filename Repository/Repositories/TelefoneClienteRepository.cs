using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories
{
    public class TelefoneClienteRepository: GenericRepository<Telefone_Cliente>
    {   
        public TelefoneClienteRepository(Context context) :base(context) {}
    }  
}