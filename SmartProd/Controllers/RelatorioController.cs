using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartProd.Models;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Fluent;

namespace SmartProd.Controllers
{
    [Authorize]
    public class RelatorioController : Controller
    {
        private readonly ContextMongodb _context = new ContextMongodb();
        private readonly UserManager<ApplicationEmpresa> _userManager;

        public RelatorioController(UserManager<ApplicationEmpresa> userManager)
        {
            _userManager = userManager;
        }

        


        public IActionResult Movimentacoes()
        {
            return View();
        }

        // Relatório da situação atual do estoque
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SituacaoEstoque()
        {
            var userId = _userManager.GetUserId(User);
            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();

            var situacao = estoques.Select(e => new
            {
                Produto = e.Produto?.Nome ?? "(Sem nome)",
                Atual = e.EstoqueAtual,
                Minimo = e.EstoqueMinima,
                Maximo = e.EstoqueMaxima,
                Status = e.EstoqueAtual < e.EstoqueMinima ? "Abaixo do Mínimo" :
                         e.EstoqueAtual > e.EstoqueMaxima ? "Acima do Máximo" : "Normal"
            });

            return View(situacao);
        }

        // Relatório de movimentações por período
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Movimentacoes(DateTime? inicio, DateTime? fim, string? periodo)
        {
            var userId = _userManager.GetUserId(User);

            if (!inicio.HasValue || !fim.HasValue)
            {
                fim = DateTime.UtcNow;

                inicio = periodo switch
                {
                    "3meses" => fim.Value.AddMonths(-3),
                    "6meses" => fim.Value.AddMonths(-6),
                    "12meses" => fim.Value.AddYears(-1),
                    _ => fim.Value.AddDays(-7) // padrão: últimos 7 dias
                };
            }

            var entradas = await _context.NotaEntrega.Find(n =>
                n.IdUsuario == userId &&
                n.DataEntrega >= inicio && n.DataEntrega <= fim
            ).ToListAsync();

            var saidas = await _context.NotaSaida.Find(n =>
                n.IdUsuario == userId &&
                n.DataSaida >= inicio && n.DataSaida <= fim
            ).ToListAsync();

            var model = new Relatorio
            {
                DataInicio = inicio.Value,
                DataFim = fim.Value,
                Entradas = entradas,
                Saidas = saidas
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ExportarPdf(DateTime? inicio, DateTime? fim)
        {
            var userId = _userManager.GetUserId(User);
            inicio ??= DateTime.UtcNow.AddMonths(-1);
            fim ??= DateTime.UtcNow;

            var entradas = await _context.NotaEntrega.Find(n =>
                n.IdUsuario == userId && n.DataEntrega >= inicio && n.DataEntrega <= fim).ToListAsync();

            var saidas = await _context.NotaSaida.Find(n =>
                n.IdUsuario == userId && n.DataSaida >= inicio && n.DataSaida <= fim).ToListAsync();

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Relatório de Movimentações de Estoque").FontSize(18).Bold().AlignCenter();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Período: {inicio:dd/MM/yyyy} - {fim:dd/MM/yyyy}");

                        col.Item().Text("Entradas").FontSize(14).Bold();
                        foreach (var entrada in entradas)
                        {
                            col.Item().Text($"- {entrada.NumeroNota} | {entrada.Fornecedor} | {entrada.DataEntrega:dd/MM/yyyy}");
                        }

                        col.Item().PaddingTop(10).Text("Saídas").FontSize(14).Bold();
                        foreach (var saida in saidas)
                        {
                            col.Item().Text($"- {saida.NumeroNota} | {saida.Destino} | {saida.DataSaida:dd/MM/yyyy}");
                        }
                    });
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;
            return File(stream, "application/pdf", "RelatorioEstoque.pdf");
        }


    }
}
