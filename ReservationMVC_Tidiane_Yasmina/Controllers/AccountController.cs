using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReservationMVC.Models;
using System.Security.Claims;

namespace ReservationMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> um, SignInManager<User> sm)
        { _userManager = um; _signInManager = sm; }

        [HttpGet]
        public IActionResult Register() => View(new RegisterInput());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterInput input)
        {
            if (!ModelState.IsValid) return View(input);

            var user = new User
            {
                UserName = input.Email,
                Email = input.Email,
                FullName = input.FullName,
                Role = input.Role
            };

            var result = await _userManager.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(input);
            }

            await _userManager.AddClaimAsync(user, new Claim("app:role",
                user.Role == AppRole.Provider ? "Provider" : "Client"));

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginInput());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInput input)
        {
            if (!ModelState.IsValid) return View(input);

            var result = await _signInManager.PasswordSignInAsync(input.Email, input.Password, false, lockoutOnFailure: false);
            if (result.Succeeded) return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Identifiants invalides.");
            return View(input);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();
    }
}
