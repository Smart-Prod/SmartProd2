namespace SmartProd.Models
{
    public class Produto
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public string? Categoria { get; set; }
        public decimal Preco { get; set; }
        public string? Imagem { get; set; }
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }

        // Adicione estas propriedades:
        public int EstoqueMinimo { get; set; }
        public int EstoqueMaximo { get; set; }
        public int EstoqueAtual { get; set; }
    }
}
