using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Driver;
using SmartProd.Models;



namespace SmartProd.Controllers
{
    [Authorize]
    public class EstoqueController : Controller
    {
        private readonly ContextMongodb _context = new ContextMongodb();
        private readonly UserManager<ApplicationEmpresa> _userManager;

        public EstoqueController(UserManager<ApplicationEmpresa> userManager)
        {
            this._userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var estoques = await _context.Estoque
                .Find(e => e.IdUsuario == userId)
                .ToListAsync();

            var produtoIds = estoques
                .Where(e => !string.IsNullOrEmpty(e.IdProduto))
                .Select(e => Guid.Parse(e.IdProduto!))
                .ToList();

            var produtos = await _context.Produto
                .Find(p => produtoIds.Contains(p.Id))
                .ToListAsync();

            foreach (var estoque in estoques)
            {
                if (!string.IsNullOrEmpty(estoque.IdProduto))
                {
                    var guidProduto = Guid.Parse(estoque.IdProduto);
                    estoque.Produto = produtos.FirstOrDefault(p => p.Id == guidProduto);
                }
            }

            // Produtos críticos agora usam Produto.EstoqueMinimo
            var produtosCriticos = estoques
                .Where(e => e.Produto != null && e.EstoqueAtual < e.Produto.EstoqueMinimo)
                .ToList();

            ViewBag.ProdutosCriticos = produtosCriticos;
            ViewBag.Produtos = await _context.Produto.Find(p => p.IdUsuario == userId).ToListAsync();
            return View(estoques);
        }
        public async Task<IActionResult> RegistrarNotaEntrega()
        {
            var userId = _userManager.GetUserId(User);
            var produtos = await _context.Produto.Find(p => p.IdUsuario == userId).ToListAsync();
            ViewBag.Produtos = produtos;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> RegistrarNotaEntrega(NotaEntrega nota)
        {
            nota.Id = Guid.NewGuid();
            var userId = _userManager.GetUserId(User);
            nota.DataEntrega = DateTime.UtcNow;
            await _context.NotaEntrega.InsertOneAsync(nota);

            foreach (var item in nota.Itens)
            {
                var filtro = Builders<Estoque>.Filter.Eq(e => e.IdProduto, item.IdProduto);
                var estoque = await _context.Estoque.Find(filtro).FirstOrDefaultAsync();

                if (estoque == null)
                {
                    estoque = new Estoque
                    {
                        IdProduto = item.IdProduto,
                        Produto = item.Produto,
                        EstoqueAtual = item.Quantidade,
                        Localizacao = "Depósito Principal",
                        DataUltimaAtualizacao = DateTime.UtcNow,
                        IdUsuario = userId
                    };
                    await _context.Estoque.InsertOneAsync(estoque);
                }
                else
                {
                    var update = Builders<Estoque>.Update
                        .Inc(e => e.EstoqueAtual, item.Quantidade)
                        .Set(e => e.DataUltimaAtualizacao, DateTime.UtcNow);

                    await _context.Estoque.UpdateOneAsync(filtro, update);
                }
            }

            return RedirectToAction("Index");
        }
        public async Task<IActionResult> RegistrarNotaSaida()
        {
            var userId = _userManager.GetUserId(User);
            var produtos = await _context.Produto.Find(p => p.IdUsuario == userId).ToListAsync();
            ViewBag.Produtos = produtos;
            return View();
        }

        // POST: /Estoque/RegistrarNotaSaida
        [HttpPost]
        public async Task<IActionResult> RegistrarNotaSaida(NotaSaida nota)
        {
            nota.Id = Guid.NewGuid();
            var userId = _userManager.GetUserId(User);
            nota.DataSaida = DateTime.UtcNow;
            await _context.NotaSaida.InsertOneAsync(nota);

            foreach (var item in nota.Itens)
            {
                var filtro = Builders<Estoque>.Filter.Eq(e => e.IdProduto, item.IdProduto);
                var estoque = await _context.Estoque.Find(filtro).FirstOrDefaultAsync();

                if (estoque != null && estoque.EstoqueAtual >= item.Quantidade)
                {
                    var update = Builders<Estoque>.Update
                        .Inc(e => e.EstoqueAtual, -item.Quantidade)
                        .Set(e => e.DataUltimaAtualizacao, DateTime.UtcNow);
                        item.IdUsuario = userId;


                    await _context.Estoque.UpdateOneAsync(filtro, update);
                }
                else
                {
                    ModelState.AddModelError("", $"Estoque insuficiente para o produto {item.IdProduto}");
                    return View("Index", await _context.Estoque.Find(_ => true).ToListAsync());
                }
            }

            return RedirectToAction("Index");
        }
        

    }
}
