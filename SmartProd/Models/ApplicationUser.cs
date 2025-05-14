using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace SmartProd.Models
{
    [CollectionName("User")]
    public class ApplicationUser:MongoIdentityUser
    {
        public string? NomeCompleto { get; set; }
    }
}
