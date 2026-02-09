using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;

namespace ReservationMVC.Controllers
{
    [Authorize]
    public class ProfilesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public ProfilesController(
            ApplicationDbContext db,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Profile));

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        private string CurrentRole =>
            User.Claims.FirstOrDefault(c => c.Type == "app:role")?.Value ?? "Client";

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var uid = CurrentUserId;

            var orders = await _db.Orders
                .Where(o => o.UserId == uid)
                .OrderByDescending(o => o.CreatedUtc)
                .Take(5)
                .ToListAsync();

            var invoices = await _db.Invoices
                .Where(i => i.BuyerUserId == uid)
                .OrderByDescending(i => i.IssuedUtc)
                .Take(5)
                .ToListAsync();

            var model = new ProfilePageModel
            {
                FullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value
                           ?? User.Identity?.Name ?? "",
                Email = User.Identity?.Name ?? "",
                Role = CurrentRole,
                RecentOrders = orders ?? new(),
                Invoices = invoices ?? new()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            var vm = new ProfileEditVm
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? ""
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return NotFound();

            user.FullName = vm.FullName?.Trim();

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["Msg"] = "Profil mis à jour.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> ClientDashboard()
        {
            var uid = CurrentUserId;

            var orders = await _db.Orders
                .Where(o => o.UserId == uid)
                .OrderByDescending(o => o.CreatedUtc)
                .Include(o => o.Lines)
                .Take(20)
                .ToListAsync();

            var invoices = await _db.Invoices
                .Where(i => i.BuyerUserId == uid)
                .OrderByDescending(i => i.IssuedUtc)
                .Take(20)
                .ToListAsync();

            var model = new ProfilePageModel
            {
                FullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value
                           ?? User.Identity?.Name ?? "",
                Email = User.Identity?.Name ?? "",
                Role = CurrentRole,
                RecentOrders = orders ?? new(),
                Invoices = invoices ?? new()
            };

            return View(model);
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpGet]
        public IActionResult ProviderDashboard() => RedirectToAction("Index", "Admin");
    }
}
