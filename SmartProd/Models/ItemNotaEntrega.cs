namespace SmartProd.Models
{
    public class ItemNotaEntrega
    {
        public string? IdProduto { get; set; }
        public Produto? Produto { get; set; }
        public int Quantidade { get; set; }
        public decimal CustoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }
    }
}
