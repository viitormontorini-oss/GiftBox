using System;
using Business;
using Microsoft.EntityFrameworkCore;
using Repository.EF;

namespace Repository.Repositories
{
    public class UsuarioRepository: GenericRepository<Usuario>
    {   
        public UsuarioRepository(Context context) :base(context) {}
    }  
}

