using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Services.Models
{
    public class CadastroSite
    {
        [StringLength(100, ErrorMessage = "O nome pode ter no máximo 100 caracteres")] // até 100 caracteres
        [Required(ErrorMessage = "Informe seu nome")] // campo precisa ser preenchido
        public string? Nome { get; set; }

        [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Digite um e-mail válido, como nome@email.com.")]
        //serve para todo email conter .com no final 

        [Required(ErrorMessage = "Informe seu e-mail.")] // email precisa ser preenchido
        [StringLength(100, ErrorMessage = "O e-mail pode ter no máximo 100 caracteres.")]
        public string? Email { get; set; }


        [MinLength(8, ErrorMessage = "A senha precisa ter pelo menos 8 caracteres")] //minimo da senha 8 caracteres
        [DataType(DataType.Password)] // type senha
        [Required(ErrorMessage = "Crie uma senha")] // senha é obrigatorio 
        public string? Senha {  get; set;}        
    }
}
