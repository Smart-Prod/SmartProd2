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

            var produtosEstoque = estoques.Select(e => new EstoqueProdutoViewModel
            {
                Nome = e.Produto?.Nome ?? "Desconhecido",
                EstoqueAtual = e.EstoqueAtual,
                EstoqueMinima = e.EstoqueMinima,
                EstoqueMaxima = e.EstoqueMaxima
            }).ToList();

            var entradasPorProduto = entradas
                .SelectMany(n => n.Itens)
                .GroupBy(i => i.Produto?.Nome)
                .Select(g => new EntradaProdutoViewModel
                {
                    Nome = g.Key ?? "Desconhecido",
                    TotalEntrada = g.Sum(x => x.Quantidade)
                }).ToList();

            var saidasPorProduto = saidas
                .SelectMany(n => n.Itens)
                .GroupBy(i => i.Produto?.Nome)
                .Select(g => new SaidaProdutoViewModel
                {
                    Nome = g.Key ?? "Desconhecido",
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
