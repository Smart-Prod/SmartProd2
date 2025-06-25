using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartProd.Models;
using SmartProd.Sevices;

namespace SmartProd.Controllers
{
    public class EmpresaController : Controller
    {
        
        private readonly ReceitaWsService _receitaWs;
        private UserManager<ApplicationEmpresa> _userManager;

        public EmpresaController(ReceitaWsService receitaWs,
            UserManager<ApplicationEmpresa> userManager)
        {
            this._userManager = userManager;
            this._receitaWs = receitaWs;

        }
        public IActionResult Cadastrar()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> BuscarCnpj(string cnpj)
        {
            try
            {
                var dados = await _receitaWs.ConsultarCnpjAsync(cnpj);
                return Json(dados);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cadastrar(Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    

                    // Monta objeto para Identity
                    ApplicationEmpresa appempresa = new ApplicationEmpresa();

                    string nomeFantasia = empresa.NomeFantasia?.Replace(" ", "") ?? "";
                    var normalizedString = nomeFantasia.Normalize(NormalizationForm.FormD);

                    StringBuilder sb = new StringBuilder();
                    foreach (char c in normalizedString)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        {
                            sb.Append(c);
                        }
                    }

                    string userName = sb.ToString().Normalize(NormalizationForm.FormC);
                    userName = Regex.Replace(userName, @"[^a-zA-Z0-9]", "");

                    appempresa.UserName = userName;
                    appempresa.Email = empresa.Email;
                    appempresa.PhoneNumber = empresa.Telefone;
                    appempresa.NomeFantasia = empresa.NomeFantasia;
                    appempresa.RazaoSocial = empresa.RazaoSocial;
                    appempresa.Cnpj = empresa.Cnpj;

                    IdentityResult result = await _userManager.CreateAsync(appempresa, empresa.Password!);

                    if (result.Succeeded)
                    {
                        ViewBag.Message = "Empresa cadastrada com sucesso";
                        return RedirectToAction("CadastroSucesso");
                    }
                    else
                    {
                        foreach (IdentityError error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Erro ao cadastrar: {ex.Message}");
                }
            }

            // Retorna a mesma view com os erros de validação
            return View(empresa);
        }
        public ActionResult CadastroSucesso()
        {
            return View();
        }


    }
}
