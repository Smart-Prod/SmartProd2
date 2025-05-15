using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using SmartProd.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartProd.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<ApplicationEmpresa>? _userManager;
        private SignInManager<ApplicationEmpresa>? _signInManager;

        public AccountController(UserManager<ApplicationEmpresa> userManager,
                                 SignInManager<ApplicationEmpresa> signInManager)
        {
            this._userManager = userManager;
            this._signInManager = signInManager;
        }
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login([Required][EmailAddress] string email,
                                               [Required] string password)
        {
            if (ModelState.IsValid)
            {
                
                ApplicationEmpresa? user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user, password, false, false);

                    if (result.Succeeded)
                    {
                        return RedirectToAction("Dashboard", "Home");
                    }
                    ModelState.AddModelError(nameof(email), "Verifique suas credenciais");
                }
            }
            return View();


        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
