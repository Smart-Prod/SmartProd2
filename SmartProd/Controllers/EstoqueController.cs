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
            var produtosCriticos = await _context.Estoque
                .Find(e => e.IdUsuario == userId && e.EstoqueAtual < e.EstoqueMinima)
                .ToListAsync();

            ViewBag.ProdutosCriticos = produtosCriticos;
            return View(await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync());
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

        // GET: /Estoque/EditarEstoque/{id}
        public async Task<IActionResult> EditarEstoque(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var estoque = await _context.Estoque.Find(e => e.Id == id && e.IdUsuario == userId).FirstOrDefaultAsync();

            if (estoque == null)
            {
                return NotFound();
            }

            return View(estoque);
        }

        // POST: /Estoque/EditarEstoque
        [HttpPost]
        public async Task<IActionResult> EditarEstoque(Estoque estoqueEditado)
        {
            var userId = _userManager.GetUserId(User);

            var filtro = Builders<Estoque>.Filter.Where(e => e.Id == estoqueEditado.Id && e.IdUsuario == userId);

            var update = Builders<Estoque>.Update
                .Set(e => e.EstoqueMinima, estoqueEditado.EstoqueMinima)
                .Set(e => e.EstoqueMaxima, estoqueEditado.EstoqueMaxima)                
                .Set(e => e.DataUltimaAtualizacao, DateTime.UtcNow);

            await _context.Estoque.UpdateOneAsync(filtro, update);

            return RedirectToAction("Index");
        }

    }
}
