namespace SmartProd.Models
{
    public class Estoque
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
        public int QuantidadeMaxima { get; set; }
    }
}
