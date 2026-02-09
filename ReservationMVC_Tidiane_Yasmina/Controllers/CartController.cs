using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;
using System.Security.Claims;

namespace ReservationMVC.Controllers
{
    [Authorize(Policy = "ClientOnly")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CartController(ApplicationDbContext db) => _db = db;

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var items = await _db.CartItems
                .Include(c => c.Offer)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var total = items.Sum(i => i.Offer!.Price * i.Quantity);
            ViewData["Total"] = total;

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int offerId)
        {
            var userId = GetUserId();

            var offerExists = await _db.Offers.AnyAsync(o => o.Id == offerId);
            if (!offerExists)
            {
                TempData["Err"] = "Offre introuvable.";
                return RedirectToAction("Index", "Offers");
            }

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.OfferId == offerId);

            if (existing != null)
                existing.Quantity++;
            else
                _db.CartItems.Add(new CartItem { OfferId = offerId, UserId = userId, Quantity = 1 });

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Article ajouté au panier.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var item = await _db.CartItems.FindAsync(id);
            if (item != null)
            {
                _db.CartItems.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Msg"] = "Article retiré du panier.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = GetUserId();
            var items = _db.CartItems.Where(c => c.UserId == userId);
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Panier vidé.";
            return RedirectToAction("Index");
        }
    }
}
