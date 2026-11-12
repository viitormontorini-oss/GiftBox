using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories
{
    public class EnderecoRepository: GenericRepository<Endereco>
    {   
        public EnderecoRepository(Context context) :base(context) {}
    }  
}