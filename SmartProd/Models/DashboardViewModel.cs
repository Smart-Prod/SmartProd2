namespace SmartProd.Models
{
    public class DashboardViewModel
    {
        public List<EstoqueProdutoViewModel> ProdutosEstoque { get; set; } = new();
        public List<EntradaProdutoViewModel> EntradasPorProduto { get; set; } = new();
        public List<SaidaProdutoViewModel> SaidasPorProduto { get; set; } = new();
        public List<EntradaNotaViewModel> EntradasPorNota { get; set; } = new();
        public List<SaidaNotaViewModel> SaidasPorNota { get; set; } = new();
        public List<MovimentacaoNotaViewModel>? MovimentacoesPorNota { get; set; }

        
            // ... já existentes
            public decimal TotalProdutosCadastrados { get; set; }
            public int TotalEstoque { get; set; }
            public int TotalEntradas { get; set; }
            public int TotalSaidas { get; set; }
            public string? ProdutoMaisEstoque { get; set; }
            public int QuantidadeMaisEstoque { get; set; }
        
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
        public Guid NotaId { get; set; }
        public string? NumeroNota { get; set; }
        public DateTime DataNota { get; set; }
        public string? Nome { get; set; }
        public int TotalEntrada { get; set; }
    }

    public class SaidaProdutoViewModel
    {
        public string? Nome { get; set; }
        public int TotalSaida { get; set; }
    }

    public class EntradaNotaViewModel
    {
        public int NotaId { get; set; }
        public string? NumeroNota { get; set; }
        public DateTime DataNota { get; set; }
        public int TotalEntrada { get; set; }
    }

    public class SaidaNotaViewModel
    {
        public int NotaId { get; set; }
        public string? NumeroNota { get; set; }
        public DateTime DataNota { get; set; }
        public decimal TotalSaida { get; set; }
        // ... outros campos se desejar
    }

    

    public class MovimentacaoNotaViewModel
    {
        public int NotaId { get; set; }
        public string? NumeroNota { get; set; }
        public DateTime DataNota { get; set; }
        public int Quantidade { get; set; } // Pode ser TotalEntrada ou TotalSaida
        public string? Tipo { get; set; } // "Entrada" ou "Saída"
    }

    public class MovimentacaoViewModel
    {
        public string? Tipo { get; set; } // "Entrada" ou "Saída"
        public string? NumeroNota { get; set; }
        public DateTime Data { get; set; }
        public string? FornecedorOuCliente { get; set; }
        public List<MovimentacaoItemViewModel>? Itens { get; set; }
    }

    public class MovimentacaoItemViewModel
    {
        public string? ProdutoNome { get; set; }
        public decimal Quantidade { get; set; }
    }
}
