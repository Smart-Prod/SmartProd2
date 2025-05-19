using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
                    // Consulta API da Receita WS para obter dados da empresa via CNPJ
                    var dadosCnpj = await _receitaWs.ConsultarCnpjAsync(empresa.Cnpj);

                    // Preenche os dados da empresa com os retornados da API
                    empresa.RazaoSocial = dadosCnpj.Nome;
                    empresa.NomeFantasia = string.IsNullOrWhiteSpace(dadosCnpj.Fantasia) ? dadosCnpj.Nome : dadosCnpj.Fantasia;
                    empresa.Email ??= dadosCnpj.Email;
                    empresa.Telefone ??= dadosCnpj.Telefone;

                    ApplicationEmpresa appempresa = new ApplicationEmpresa();

                    string nomeFantasia = empresa.NomeFantasia?.Replace(" ", "") ?? "";
                    var normalizedString = nomeFantasia.Normalize(NormalizationForm.FormD);

                    // Remove os caracteres de acentuação
                    StringBuilder sb = new StringBuilder();
                    foreach (char c in normalizedString)
                    {
                        if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                        {
                            sb.Append(c);
                        }
                    }

                    string userName = sb.ToString().Normalize(NormalizationForm.FormC);

                    // Remove caracteres especiais
                    userName = Regex.Replace(userName, @"[^a-zA-Z0-9]", "");

                    appempresa.UserName = userName;
                    appempresa.Email = empresa.Email;
                    appempresa.PhoneNumber = empresa.Telefone;
                    appempresa.NomeFantasia = empresa.NomeFantasia;
                    appempresa.RazaoSocial = empresa.RazaoSocial;
                    appempresa.Cnpj = empresa.Cnpj;

                    IdentityResult result = await _userManager.CreateAsync(appempresa, empresa.Password);

                    if (result.Succeeded)
                    {
                        ViewBag.Message = "Empresa cadastrada com sucesso";
                        return RedirectToAction("Login", "Account");
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
                    ModelState.AddModelError("", $"Erro ao consultar CNPJ: {ex.Message}");
                }
            }

            // Retorna a mesma view com os erros de validação
            return View(empresa);
        }
            
        
    }
}
