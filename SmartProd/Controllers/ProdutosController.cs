using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SmartProd.Models;

namespace SmartProd.Controllers
{
    [Authorize]
    public class ProdutosController : Controller
    {
        private readonly ContextMongodb _context = new ContextMongodb();
        private readonly UserManager<ApplicationUser> _userManager;

        public ProdutosController(UserManager<ApplicationUser> userManager)
        {
            this._userManager = userManager;
        }

        // GET: Produtos
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            return View(await _context.Produto.Find(p => p.IdUsuario == userId).ToListAsync());
            
        }

        // GET: Produtos/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var userId = _userManager.GetUserId(User);
            var produto = await _context.Produto.Find(p => p.IdUsuario == userId).FirstOrDefaultAsync();
            if (produto == null) return NotFound();

            return View(produto);
        }

        // GET: Produtos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Produtos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produto produto, IFormFile? Imagem)
        {
            if (ModelState.IsValid)
            {
                produto.Id = Guid.NewGuid();
                var userId = _userManager.GetUserId(User);
                produto.IdUsuario = userId;

                if (Imagem != null && Imagem.Length > 0)
                {
                    var fileName = Path.GetFileName(Imagem.FileName);
                    var imgPath = Path.Combine("img", fileName);
                    produto.Imagem = imgPath;

                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imgPath);
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await Imagem.CopyToAsync(stream);
                }

                await _context.Produto.InsertOneAsync(produto);
                return RedirectToAction(nameof(Index));
            }

            return View(produto);
        }

        // GET: Produtos/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            
            var produto = await _context.Produto.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (produto == null) return NotFound();

            return View(produto);
        }

        // POST: Produtos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Produto produto, string? oldimage, IFormFile? Imagem)
        {
            if (id != produto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                produto.IdUsuario = userId;

                if (Imagem != null && Imagem.Length > 0)
                {
                    var fileName = Path.GetFileName(Imagem.FileName);
                    var newImgPath = Path.Combine("img", fileName);
                    produto.Imagem = newImgPath;

                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", newImgPath);
                    using var stream = new FileStream(fullPath, FileMode.Create);
                    await Imagem.CopyToAsync(stream);

                    // Excluir imagem antiga
                    if (!string.IsNullOrEmpty(oldimage))
                    {
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldimage);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                }
                else
                {
                    produto.Imagem = oldimage;
                }

                await _context.Produto.ReplaceOneAsync(p => p.Id == produto.Id, produto);
                return RedirectToAction(nameof(Index));
            }

            return View(produto);
        }

        // GET: Produtos/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            
            var produto = await _context.Produto.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (produto == null) return NotFound();

            return View(produto);
        }

        // POST: Produtos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id, string? Imagem)
        {
            await _context.Produto.DeleteOneAsync(p => p.Id == id);
            
            if (!string.IsNullOrEmpty(Imagem))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", Imagem);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
