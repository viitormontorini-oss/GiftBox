using Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Repository.Repositories;

namespace Services
{
    public class EnderecoServices: GenericServices<Endereco>
    {
        public EnderecoServices(GenericRepository<Endereco> repository) : base(repository) { }
    }
}