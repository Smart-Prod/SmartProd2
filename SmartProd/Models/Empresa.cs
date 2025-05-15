using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartProd.Models
{
   
    public class Empresa
    {
        public Guid Id { get; set; }
        [Required]
        public string? Cnpj { get; set; }
        [Required]
        public string? NomeFantasia { get; set; }
        [Required]
        public string? RazaoSocial { get; set; }
        public string? Telefone { get; set; }
        [Required, EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }

    }
}
