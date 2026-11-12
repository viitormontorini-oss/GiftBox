using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Services.Models
{
    public class LoginSite
    {
        [EmailAddress(ErrorMessage = "Digite um e-mail válido, como nome@email.com.")] //email precisa contem @
        [Required(ErrorMessage = "Informe seu e-mail.")] // email precisa ser preenchido
        [StringLength(100, ErrorMessage = "O e-mail pode ter no máximo 100 caracteres.")]
        public string? Email { get; set; }

        [DataType(DataType.Password)] // type senha
        [Required(ErrorMessage = "Informe sua senha")] // senha é obrigatorio 
        public string? Senha { get; set; }

    }
          


}
