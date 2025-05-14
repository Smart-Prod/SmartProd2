using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProd.Models
{
   
    public class Usuario
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Nome Completo")]
        public string? NomeCompleto { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "E-mail inválido")]
        public string? Email { get; set; }
        [Required]
        public string? Telefone { get; set; }

        [Required]
        public string? Password { get; set; }

    }
}
