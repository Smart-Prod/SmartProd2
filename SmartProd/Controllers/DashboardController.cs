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

            var produtoIdsEstoque = estoques.Where(e => !string.IsNullOrEmpty(e.IdProduto)).Select(e => Guid.Parse(e.IdProduto!));
            var produtoIdsEntradas = entradas.SelectMany(n => n.Itens).Where(i => !string.IsNullOrEmpty(i.IdProduto)).Select(i => Guid.Parse(i.IdProduto!));
            var produtoIdsSaidas = saidas.SelectMany(n => n.Itens).Where(i => !string.IsNullOrEmpty(i.IdProduto)).Select(i => Guid.Parse(i.IdProduto!));
            var todosProdutoIds = produtoIdsEstoque.Concat(produtoIdsEntradas).Concat(produtoIdsSaidas).Distinct().ToList();

            var produtos = await _context.Produto.Find(p => todosProdutoIds.Contains(p.Id)).ToListAsync();
            var produtosDict = produtos.ToDictionary(p => p.Id, p => p);

            foreach (var estoque in estoques)
            {
                if (!string.IsNullOrEmpty(estoque.IdProduto))
                {
                    var guidProduto = Guid.Parse(estoque.IdProduto);
                    estoque.Produto = produtosDict.ContainsKey(guidProduto) ? produtosDict[guidProduto] : null;
                }
            }

            // Associar Produto a cada item de entrada
            foreach (var entrada in entradas)
            {
                foreach (var item in entrada.Itens)
                {
                    if (!string.IsNullOrEmpty(item.IdProduto))
                    {
                        var guidProduto = Guid.Parse(item.IdProduto);
                        item.Produto = produtosDict.ContainsKey(guidProduto) ? produtosDict[guidProduto] : null;
                    }
                }
            }

            // Associar Produto a cada item de saída
            foreach (var saida in saidas)
            {
                foreach (var item in saida.Itens)
                {
                    if (!string.IsNullOrEmpty(item.IdProduto))
                    {
                        var guidProduto = Guid.Parse(item.IdProduto);
                        item.Produto = produtosDict.ContainsKey(guidProduto) ? produtosDict[guidProduto] : null;
                    }
                }
            }

            // Montar os ViewModels com nomes preenchidos
            var produtosEstoque = estoques.Select(e => new EstoqueProdutoViewModel
            {
                Nome = e.Produto?.Nome ?? "Produto não encontrado",
                EstoqueAtual = e.EstoqueAtual,
                EstoqueMinimo = e.Produto?.EstoqueMinimo ?? 0,
                EstoqueMaximo = e.Produto?.EstoqueMaximo ?? 0,
                
            }).ToList();

            var entradasPorProduto = entradas
       .SelectMany(n => n.Itens)
       .Where(i => i.Produto != null)
       .GroupBy(i => i.Produto!.Nome)
       .Select(g => new EntradaProdutoViewModel
       {
           Nome = g.Key,
           TotalEntrada = g.Sum(x => x.Quantidade)
       }).ToList();

            var saidasPorProduto = saidas
                .SelectMany(n => n.Itens)
                .Where(i => i.Produto != null)
                .GroupBy(i => i.Produto!.Nome)
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
