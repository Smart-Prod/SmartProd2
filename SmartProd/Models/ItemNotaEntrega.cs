namespace SmartProd.Models
{
    public class ItemNotaEntrega
    {
        public string? IdProduto { get; set; }
        public Produto? Produto { get; set; }
        public int Quantidade { get; set; }
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }
    }
}
