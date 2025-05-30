namespace SmartProd.Models
{
    public class DashboardViewModel
    {
        public List<EstoqueProdutoViewModel> ProdutosEstoque { get; set; } = new();
        public List<EntradaProdutoViewModel> EntradasPorProduto { get; set; } = new();
        public List<SaidaProdutoViewModel> SaidasPorProduto { get; set; } = new();
    }

    public class EstoqueProdutoViewModel
    {
        public string? Nome { get; set; }
        public int EstoqueAtual { get; set; }
        public int EstoqueMinimo { get; set; }
        public int EstoqueMaximo { get; set; }
        public bool EstoqueBaixo => EstoqueAtual < EstoqueMinimo;
    }

    public class EntradaProdutoViewModel
    {
        public string? Nome { get; set; }
        public int TotalEntrada { get; set; }
    }

    public class SaidaProdutoViewModel
    {
        public string? Nome { get; set; }
        public int TotalSaida { get; set; }
    }

}
