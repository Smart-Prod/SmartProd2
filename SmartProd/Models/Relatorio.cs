namespace SmartProd.Models
{
    public class Relatorio
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public List<NotaEntrega> Entradas { get; set; } = new();
        public List<NotaSaida> Saidas { get; set; } = new();
    }
}
