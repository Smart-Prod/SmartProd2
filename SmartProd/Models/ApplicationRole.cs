using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace SmartProd.Models
{
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole
    {
    }
}
