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

            var estoques = await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync();
            var entradas = await _context.NotaEntrega.Find(e => e.IdUsuario == userId).ToListAsync();
            var saidas = await _context.NotaSaida.Find(e => e.IdUsuario == userId).ToListAsync();

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
            var entradasPorNota = entradas
                .Select(n => new EntradaNotaViewModel
                {
                    NotaId = n.Id.GetHashCode(), // Convert Guid to int using GetHashCode
                    NumeroNota = n.NumeroNota,
                    DataNota = n.DataEntrega, // Corrected property name to match NotaEntrega
                    TotalEntrada = n.Itens.Sum(x => x.Quantidade)
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
            var saidasPorNota = saidas
                .Select(n => new SaidaNotaViewModel
                {
                    NotaId = n.Id.GetHashCode(),
                    NumeroNota = n.NumeroNota,
                    DataNota = n.DataSaida, // Supondo que NotaSaida tenha DataSaida
                    TotalSaida = n.Itens.Sum(x => x.Quantidade)
                }).ToList();
            var movimentacoesPorNota = new List<MovimentacaoNotaViewModel>();

            movimentacoesPorNota.AddRange(entradas.Select(n => new MovimentacaoNotaViewModel
            {
                Tipo = "Entrada",
                NotaId = n.Id.GetHashCode(),
                NumeroNota = n.NumeroNota!,
                DataNota = n.DataEntrega,
                Quantidade = n.Itens.Sum(x => x.Quantidade)
            }));

            movimentacoesPorNota.AddRange(saidas.Select(n => new MovimentacaoNotaViewModel
            {
                Tipo = "Saída",
                NotaId = n.Id.GetHashCode(),
                NumeroNota = n.NumeroNota!,
                DataNota = n.DataSaida,
                Quantidade = n.Itens.Sum(x => x.Quantidade)
            }));

            movimentacoesPorNota = movimentacoesPorNota.OrderByDescending(m => m.DataNota).ToList();

            // Soma do valor de todos os produtos cadastrados
            var totalProdutosCadastrados = produtos.Count;
            

            // Soma total de itens em estoque
            var totalEstoque = estoques.Sum(e => e.EstoqueAtual);

            // Quantidade de notas de entrada
            var totalEntradas = entradas.Count;

            // Quantidade de notas de saída
            var totalSaidas = saidas.Count;

            // Produto com mais estoque
            var estoqueMaisAlto = estoques.OrderByDescending(e => e.EstoqueAtual).FirstOrDefault();
            var produtoMaisEstoque = estoqueMaisAlto?.Produto?.Nome ?? "N/A";
            var quantidadeMaisEstoque = estoqueMaisAlto?.EstoqueAtual ?? 0;

            var viewModel = new DashboardViewModel
            {
                ProdutosEstoque = produtosEstoque,
                EntradasPorProduto = entradasPorProduto,
                SaidasPorProduto = saidasPorProduto,
                EntradasPorNota = entradasPorNota,
                SaidasPorNota = saidasPorNota,
                MovimentacoesPorNota = movimentacoesPorNota,
                TotalProdutosCadastrados = totalProdutosCadastrados,                
                TotalEstoque = totalEstoque,
                TotalEntradas = totalEntradas,
                TotalSaidas = totalSaidas,
                ProdutoMaisEstoque = produtoMaisEstoque,
                QuantidadeMaisEstoque = quantidadeMaisEstoque
            };

            return View(viewModel);
        }

        public async Task<IActionResult> DetalhesNotaEntrada(Guid id)
        {
            var notaEntrada = await _context.NotaEntrega.Find(e => e.Id == id).FirstOrDefaultAsync();
            // Carregue os itens e produtos se necessário
            return PartialView("_DetalhesNotaEntrada", notaEntrada);
        }

        public async Task<IActionResult> DetalhesNotaSaida(Guid id)
        {
            var notaSaida = await _context.NotaSaida.Find(e => e.Id == id).FirstOrDefaultAsync();
            // Carregue os itens e produtos se necessário
            return PartialView("_DetalhesNotaSaida", notaSaida);
        }
    }
}
