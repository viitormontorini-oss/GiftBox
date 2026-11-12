using Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Repository.Repositories;

namespace Services
{
    public class Telefone_ClienteServices: GenericServices<Telefone_Cliente>
    {
        public Telefone_ClienteServices(GenericRepository<Telefone_Cliente> repository) : base(repository) { }
    }
}