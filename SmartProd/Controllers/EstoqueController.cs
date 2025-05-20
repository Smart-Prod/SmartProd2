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
            
            return View(await _context.Estoque.Find(e => e.IdUsuario == userId).ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> RegistrarNotaEntrega(NotaEntrega nota)
        {
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
                        DataUltimaAtualizacao = DateTime.UtcNow
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

        // POST: /Estoque/RegistrarNotaSaida
        [HttpPost]
        public async Task<IActionResult> RegistrarNotaSaida(NotaSaida nota)
        {
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
