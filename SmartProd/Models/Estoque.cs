namespace SmartProd.Models
{
    public class Estoque
    {
        public Guid Id { get; set; }
        public string? IdProduto { get; set; }
        public Produto? Produto { get; set; }
        public int EstoqueAtual { get; set; }
        public int EstoqueMinima { get; set; }
        public int EstoqueMaxima { get; set; }
        public string? Localizacao { get; set; }
        public DateTime DataUltimaAtualizacao { get; set; } = DateTime.UtcNow;
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }
    }
}
