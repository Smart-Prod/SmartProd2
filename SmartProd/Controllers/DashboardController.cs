using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using SmartProd.Models;

namespace SmartProd.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ContextMongodb _context = new ContextMongodb();
        private readonly UserManager<ApplicationEmpresa> _userManager;

        public DashboardController(UserManager<ApplicationEmpresa> userManager)
        {
            this._userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var estoques = await _context.Estoque.Find(_ => true).ToListAsync();
            var entradas = await _context.NotaEntrega.Find(_ => true).ToListAsync();
            var saidas = await _context.NotaSaida.Find(_ => true).ToListAsync();

            // Busque todos os produtos. Considere que Produto.Id é Guid
            var produtoIds = estoques
                .Where(e => !string.IsNullOrEmpty(e.IdProduto))
                .Select(e => Guid.Parse(e.IdProduto))
                .Union(entradas.SelectMany(n => n.Itens).Where(i => !string.IsNullOrEmpty(i.IdProduto)).Select(i => Guid.Parse(i.IdProduto)))
                .Union(saidas.SelectMany(n => n.Itens).Where(i => !string.IsNullOrEmpty(i.IdProduto)).Select(i => Guid.Parse(i.IdProduto)))
                .Distinct()
                .ToList();

            var produtos = await _context.Produto.Find(p => produtoIds.Contains(p.Id)).ToListAsync();

            // Associe manualmente Produto em Estoque
            foreach (var e in estoques)
            {
                if (!string.IsNullOrEmpty(e.IdProduto))
                    e.Produto = produtos.FirstOrDefault(p => p.Id == Guid.Parse(e.IdProduto));
            }

            // Associe manualmente Produto em entradas e saidas
            foreach (var n in entradas)
            {
                foreach (var i in n.Itens)
                {
                    if (!string.IsNullOrEmpty(i.IdProduto))
                        i.Produto = produtos.FirstOrDefault(p => p.Id == Guid.Parse(i.IdProduto));
                }
            }
            foreach (var n in saidas)
            {
                foreach (var i in n.Itens)
                {
                    if (!string.IsNullOrEmpty(i.IdProduto))
                        i.Produto = produtos.FirstOrDefault(p => p.Id == Guid.Parse(i.IdProduto));
                }
            }

            var produtosEstoque = estoques.Select(e => new EstoqueProdutoViewModel
            {
                Nome = e.Produto?.Nome,
                EstoqueAtual = e.EstoqueAtual,
                EstoqueMinima = e.EstoqueMinima,
                EstoqueMaxima = e.EstoqueMaxima
            }).ToList();

            var entradasPorProduto = entradas
                .SelectMany(n => n.Itens)
                .GroupBy(i => i.Produto?.Nome)
                .Select(g => new EntradaProdutoViewModel
                {
                    Nome = g.Key,
                    TotalEntrada = g.Sum(x => x.Quantidade)
                }).ToList();

            var saidasPorProduto = saidas
                .SelectMany(n => n.Itens)
                .GroupBy(i => i.Produto?.Nome)
                .Select(g => new SaidaProdutoViewModel
                {
                    Nome = g.Key,
                    TotalSaida = g.Sum(x => x.Quantidade)
                }).ToList();

            var viewModel = new DashboardViewModel
            {
                ProdutosEstoque = produtosEstoque,
                EntradasPorProduto = entradasPorProduto,
                SaidasPorProduto = saidasPorProduto
            };

            return View(viewModel);
        }
    }
}
