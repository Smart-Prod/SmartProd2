using System.Drawing;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartProd.Models;

namespace SmartProd.Controllers
{
    [Authorize]
    public partial class ProdutosController : Controller
    {
        private readonly ContextMongodb _context = new ContextMongodb();
        private readonly UserManager<ApplicationEmpresa> _userManager;

        public ProdutosController(UserManager<ApplicationEmpresa> userManager)
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
                    produto.Imagem = Path.Combine("img", fileName);

                    var imgDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
                    if (!Directory.Exists(imgDir))
                        Directory.CreateDirectory(imgDir);

                    var filePath = Path.Combine(imgDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await Imagem.CopyToAsync(stream);
                    }
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] DeleteMultipleDto model)
        {
            var selectedIds = model.ids;
            if (selectedIds == null || !selectedIds.Any())
                return Json(new { success = false, message = "Nenhum produto selecionado." });

            var filter = Builders<Produto>.Filter.In(p => p.Id, selectedIds);
            var produtos = await _context.Produto.Find(filter).ToListAsync();

            var produtosComEstoque = produtos.Where(p => p.EstoqueAtual != 0).ToList();
            if (produtosComEstoque.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Existem produtos com estoque diferente de zero.",
                    idsComEstoque = produtosComEstoque.Select(p => p.Id)
                });
            }

            // Deletar imagens
            foreach (var produto in produtos)
            {
                if (!string.IsNullOrEmpty(produto.Imagem))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", produto.Imagem);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
            }

            await _context.Produto.DeleteManyAsync(filter);
            // 🔥 Deletar o estoque relacionado a esses produtos
            var filterEstoque = Builders<Estoque>.Filter.In(e => e.Produto.Id, selectedIds);
            await _context.Estoque.DeleteManyAsync(filterEstoque);

            return Json(new { success = true });
        }


    }
}
