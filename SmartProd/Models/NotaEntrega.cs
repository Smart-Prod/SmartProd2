

namespace SmartProd.Models
{
    public class NotaEntrega
    {
        public Guid Id { get; set; }       
        public string? NumeroNota { get; set; }
        public DateTime DataEntrega { get; set; }
        public string? Fornecedor { get; set; }
        public List<ItemNotaEntrega> Itens { get; set; } = new();
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }
    }
}
