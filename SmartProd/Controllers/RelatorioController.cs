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
        public IActionResult Index()
        {
            return View();
        }
        private DateTime ObterDataInicial(string periodo)
        {
            var agora = DateTime.UtcNow;
            return periodo switch
            {
                "3meses" => agora.AddMonths(-3),
                "6meses" => agora.AddMonths(-6),
                "1ano" => agora.AddYears(-1),
                _ => agora.AddMonths(-3)
            };
        }

        // 1. Saldo de Estoque (quantidade atual)
        [HttpGet("saldo")]
        public async Task<IActionResult> Saldo()
        {
            var userId = _userManager.GetUserId(User);
            var estoques = await _context.Estoque.Find(_ => true).ToListAsync();
            var produtos = await _context.Produto.Find(_ => true).ToListAsync();
            var lista = estoques.Select(e => new EstoqueProdutoViewModel
            {
                Nome = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.Nome,
                EstoqueAtual = e.EstoqueAtual,
                EstoqueMinimo = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.EstoqueMinimo ?? 0,
                EstoqueMaximo = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.EstoqueMaximo ?? 0
            }).ToList();

            return View(lista);
        }

        // 2. Relatório de Movimentações (entradas e saídas no período)
        [HttpGet("movimentacoes")]
        public async Task<IActionResult> Movimentacoes(DateTime? dataInicial, DateTime? dataFinal)
        {
            var userId = _userManager.GetUserId(User);
            var agora = DateTime.UtcNow;
            // Valores padrão se não enviados
            dataInicial ??= agora.AddMonths(-3);
            dataFinal ??= agora;

            var entradas = await _context.NotaEntrega
                .Find(n => n.DataEntrega >= dataInicial && n.DataEntrega <= dataFinal)
                .ToListAsync();

            var saidas = await _context.NotaSaida
                .Find(n => n.DataSaida >= dataInicial && n.DataSaida <= dataFinal)
                .ToListAsync();

            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            foreach (var nota in entradas)
            {
                foreach (var item in nota.Itens)
                {
                    item.Produto = produtos.FirstOrDefault(p => p.Id.ToString() == item.IdProduto);
                }
            }

            var movimentacoes = new
            {
                Entradas = entradas,
                Saidas = saidas
            };
            var movimentacoesUnificadas = new List<MovimentacaoViewModel>();

            movimentacoesUnificadas.AddRange(entradas.Select(e => new MovimentacaoViewModel
            {
                Tipo = "Entrada",
                NumeroNota = e.NumeroNota!,
                Data = e.DataEntrega,
                FornecedorOuCliente = e.Fornecedor!,
                Itens = e.Itens.Select(i => new MovimentacaoItemViewModel
                {
                    ProdutoNome = i.Produto?.Nome!,
                    Quantidade = i.Quantidade
                }).ToList()
            }));

            movimentacoesUnificadas.AddRange(saidas.Select(s => new MovimentacaoViewModel
            {
                Tipo = "Saída",
                NumeroNota = s.NumeroNota!,
                Data = s.DataSaida,
                FornecedorOuCliente = s.Cliente!, // ou Destinatario
                Itens = s.Itens.Select(i => new MovimentacaoItemViewModel
                {
                    ProdutoNome = i.Produto?.Nome!,
                    Quantidade = i.Quantidade
                }).ToList()
            }));

            // Ordene por data (opcional)
            movimentacoesUnificadas = movimentacoesUnificadas
                .OrderByDescending(m => m.Data)
                .ToList();

            ViewBag.DataInicial = dataInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal.Value.ToString("yyyy-MM-dd");

            return View(movimentacoesUnificadas);
        }

        // 3. Relatório de Custo (custo total de entradas no período)
        [HttpGet("custo")]
        public async Task<IActionResult> Custo(DateTime? dataInicial, DateTime? dataFinal)
        {
            var userId = _userManager.GetUserId(User);
            var agora = DateTime.UtcNow;
            // Valores padrão se não enviados
            dataInicial ??= agora.AddMonths(-3);
            dataFinal ??= agora;
            var entradas = await _context.NotaEntrega
                .Find(n => n.DataEntrega >= dataInicial && n.DataEntrega <= dataFinal)
                .ToListAsync();
            // Populando produtos:
            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            foreach (var nota in entradas)
            {
                foreach (var item in nota.Itens)
                {
                    item.Produto = produtos.FirstOrDefault(p => p.Id.ToString() == item.IdProduto);
                }
            }
            var totalCusto = entradas.SelectMany(n => n.Itens).Sum(i => i.ValorTotal);

            ViewBag.TotalCusto = totalCusto;
            ViewBag.DataInicial = dataInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal.Value.ToString("yyyy-MM-dd");

            return View(entradas);
        }

        // 4. Inventário (estoque atual detalhado)
        [HttpGet("inventario")]
        public async Task<IActionResult> Inventario()
        {
            var userId = _userManager.GetUserId(User);
            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();
            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            var inventario = estoques.Select(e => new
            {
                Produto = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto),
                e.EstoqueAtual,
                e.Localizacao,
                e.DataUltimaAtualizacao
            }).ToList();

            return View(inventario);
        }



        // 6. Curva ABC (baseada no valor movimentado em saídas)
        [HttpGet("curva-abc")]
        public async Task<IActionResult> CurvaABC(DateTime? dataInicial, DateTime? dataFinal)
        {
            var userId = _userManager.GetUserId(User);
            var agora = DateTime.UtcNow;
            // Valores padrão se não enviados
            dataInicial ??= agora.AddMonths(-3);
            dataFinal ??= agora;

            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            var saidas = await _context.NotaSaida
                .Find(n => n.DataSaida >= dataInicial && n.DataSaida <= dataFinal)
                .ToListAsync();

            var grupoSaidas = saidas
                .SelectMany(n => n.Itens)
                .GroupBy(i => i.IdProduto)
                .Select(g =>
                {
                    var produto = produtos.FirstOrDefault(p => p.Id.ToString() == g.Key);
                    var valor = g.Sum(x => x.Quantidade) * (produto?.Preco ?? 0);
                    return new { Produto = produto, Valor = valor };
                })
                .OrderByDescending(x => x.Valor)
                .ToList();

            decimal total = grupoSaidas.Sum(x => x.Valor);
            decimal acumulado = 0;
            var curva = grupoSaidas.Select(x =>
            {
                acumulado += x.Valor;
                string categoria = acumulado / total <= 0.8m ? "A" : (acumulado / total <= 0.95m ? "B" : "C");
                return new { x.Produto, x.Valor, Categoria = categoria };
            }).ToList();

            ViewBag.DataInicial = dataInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal.Value.ToString("yyyy-MM-dd");

            return View(curva);
        }

        // 7. Giro de Estoque (saídas/estoque médio no período)
        [HttpGet("giro")]
        public async Task<IActionResult> Giro(DateTime? dataInicial, DateTime? dataFinal)
        {
            var userId = _userManager.GetUserId(User);

            // Define valores padrão se não enviados
            var agora = DateTime.UtcNow;
            dataInicial ??= agora.AddYears(-1);
            dataFinal ??= agora;

            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();
            var saidas = await _context.NotaSaida
                .Find(n => n.DataSaida >= dataInicial && n.DataSaida <= dataFinal)
                .ToListAsync();

            var giro = produtos.Select(p =>
            {
                var estoque = estoques.FirstOrDefault(e => e.IdProduto == p.Id.ToString());
                var saidasProduto = saidas
                    .SelectMany(n => n.Itens)
                    .Where(i => i.IdProduto == p.Id.ToString())
                    .Sum(i => i.Quantidade);

                var estoqueMedio = estoque?.EstoqueAtual ?? 0; // Você pode calcular a média real se quiser
                return new
                {
                    Produto = p,
                    Giro = estoqueMedio > 0 ? (decimal)saidasProduto / estoqueMedio : 0
                };
            }).OrderByDescending(x => x.Giro).ToList();

            ViewBag.DataInicial = dataInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal.Value.ToString("yyyy-MM-dd");

            return View(giro);
        }

        // 8. Produtos Parados (sem saída no período e com estoque > 0)
        [HttpGet("produtos-parados")]
        public async Task<IActionResult> ProdutosParados(DateTime? dataInicial, DateTime? dataFinal)
        {
            var userId = _userManager.GetUserId(User);
            var agora = DateTime.UtcNow;
            // Se não informado, padrão: últimos 6 meses
            dataInicial ??= agora.AddMonths(-6);
            dataFinal ??= agora;

            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();
            var saidas = await _context.NotaSaida
                .Find(n => n.DataSaida >= dataInicial && n.DataSaida <= dataFinal)
                .ToListAsync();
            var produtosMovimentados = saidas
                .SelectMany(n => n.Itens)
                .Select(i => i.IdProduto)
                .Distinct()
                .ToList();

            var parados = estoques
                .Where(e => !produtosMovimentados.Contains(e.IdProduto) && e.EstoqueAtual > 0)
                .Select(e => produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto))
                .ToList();

            // Passa datas para a View para exibir no filtro
            ViewBag.DataInicial = dataInicial.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFinal = dataFinal.Value.ToString("yyyy-MM-dd");

            return View(parados);
        }


                [HttpGet("resumo-estoque")]
        public async Task<IActionResult> ResumoEstoque()
        {
            var userId = _userManager.GetUserId(User);
            // Saldo
            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();
            var produtos = await _context.Produto.Find(e => e.IdUsuario == userId).ToListAsync();
            var listaSaldo = estoques.Select(e => new EstoqueProdutoViewModel
            {
                Nome = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.Nome,
                EstoqueAtual = e.EstoqueAtual,
                EstoqueMinimo = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.EstoqueMinimo ?? 0,
                EstoqueMaximo = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto)?.EstoqueMaximo ?? 0
            }).ToList();

            // Inventário
            var listaInventario = estoques.Select(e => new
            {
                Produto = produtos.FirstOrDefault(p => p.Id.ToString() == e.IdProduto),
                e.EstoqueAtual,
                e.Localizacao,
                e.DataUltimaAtualizacao
            }).ToList();

            // Mínimos e máximos
            var produtosAbaixoMinimo = estoques
                .Where(e => {
                    var p = produtos.FirstOrDefault(x => x.Id.ToString() == e.IdProduto);
                    return p != null && e.EstoqueAtual < p.EstoqueMinimo;
                })
                .Select(e => produtos.FirstOrDefault(x => x.Id.ToString() == e.IdProduto))
                .ToList();

            var produtosAcimaMaximo = estoques
                .Where(e => {
                    var p = produtos.FirstOrDefault(x => x.Id.ToString() == e.IdProduto);
                    return p != null && e.EstoqueAtual > p.EstoqueMaximo;
                })
                .Select(e => produtos.FirstOrDefault(x => x.Id.ToString() == e.IdProduto))
                .ToList();

            return View("ResumoEstoque", new
            {
                Saldo = listaSaldo,
                Inventario = listaInventario,
                Minimos = produtosAbaixoMinimo,
                Maximos = produtosAcimaMaximo
            });
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
