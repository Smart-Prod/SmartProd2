﻿

namespace SmartProd.Models
{
    public class NotaSaida
    {
        public Guid Id { get; set; }        
        public string? NumeroNota { get; set; }        
        public DateTime DataSaida { get; set; }
        public string? Cliente { get; set; }
        public string? Destino { get; set; }        
        public List<ItemNotaSaida> Itens { get; set; } = new();
        public string? IdUsuario { get; set; }
        public Empresa? Usuario { get; set; }
    }
}
