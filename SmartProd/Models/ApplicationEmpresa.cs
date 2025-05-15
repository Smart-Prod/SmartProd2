using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace SmartProd.Models
{
    [CollectionName("Empresa")]
    public class ApplicationEmpresa:MongoIdentityUser
    {
        public string? RazaoSocial { get; set; }
        public string? NomeFantasia { get; set; }
        public string? Cnpj { get; set; }
    }
}
