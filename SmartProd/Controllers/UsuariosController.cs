using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartProd.Models;

namespace SmartProd.Controllers
{
    public class UsuariosController : Controller
    {
      
        private UserManager<ApplicationUser> _userManager;

        public UsuariosController(UserManager<ApplicationUser> userManager)
        {
            this._userManager = userManager;
        }
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser appuser = new ApplicationUser();
                string userName = usuario.NomeCompleto.Replace(" ", "");
                var normalizedString = userName.Normalize(NormalizationForm.FormD);

                // Remove os caracteres de acentuação
                StringBuilder sb = new StringBuilder();
                foreach (char c in normalizedString)
                {
                    // Apenas mantém os caracteres que não são diacríticos (acentos, til, etc.)
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    {
                        sb.Append(c);
                    }
                }

                userName = sb.ToString().Normalize(NormalizationForm.FormC);

                userName = Regex.Replace(userName, @"[^a-zA-Z0-9\s]", "");

                Console.WriteLine(userName);

                appuser.UserName = userName;
                appuser.Email = usuario.Email;
                appuser.PhoneNumber = usuario.Telefone;
                appuser.NomeCompleto = usuario.NomeCompleto;
                IdentityResult result = await _userManager.CreateAsync(appuser, usuario.Password);
                if (result.Succeeded)
                {
                    ViewBag.Message = "Usuário Cadastrado com sucesso";
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View();
        }
    }
}
