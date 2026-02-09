using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;
using System.Security.Claims;

namespace ReservationMVC.Controllers
{
    public class OffersController : Controller
    {
        private static readonly string[] AllowedCategories = { "Cinéma", "Sport", "Hôtel", "Loisirs" };
        private readonly ApplicationDbContext _db;

        public OffersController(ApplicationDbContext db) => _db = db;

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [AllowAnonymous]
        public async Task<IActionResult> Index([FromQuery] OfferFilter f)
        {
            IQueryable<Offer> q = _db.Offers.AsNoTracking()
                .Where(o => o.Category != null && AllowedCategories.Contains(o.Category));

            if (f.FutureOnly) q = q.Where(o => o.DateTimeUtc >= DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(f.Q))
            {
                var term = $"%{f.Q.Trim()}%";
                q = q.Where(o =>
                    EF.Functions.Like(o.Title, term) ||
                    EF.Functions.Like(o.Description ?? "", term) ||
                    EF.Functions.Like(o.Location ?? "", term));
            }
            if (!string.IsNullOrWhiteSpace(f.Category))
            {
                var cat = AllowedCategories.FirstOrDefault(c =>
                    string.Equals(c, f.Category.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cat != null)
                    q = q.Where(o => o.Category == cat);
            }

            switch (f.Sort?.ToLowerInvariant())
            {
                case "date_desc":
                    q = q.OrderByDescending(o => o.DateTimeUtc ?? DateTime.MaxValue);
                    break;
                case "price_asc":
                    q = q.OrderBy(o => o.Price);
                    break;
                case "price_desc":
                    q = q.OrderByDescending(o => o.Price);
                    break;
                case "title_asc":
                    q = q.OrderBy(o => o.Title);
                    break;
                default:
                    q = q.OrderBy(o => o.DateTimeUtc ?? DateTime.MaxValue);
                    break;
            }

            var offers = await q.ToListAsync();
            ViewBag.AllowedCategories = AllowedCategories;
            ViewData["Filter"] = f;
            return View(offers);
        }

        [AllowAnonymous]
      
        public async Task<IActionResult> Details(int id)
        {
            var offer = await _db.Offers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null) return NotFound();

            bool canEdit = false;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var isProvider = User.Claims.Any(c => c.Type == "app:role" && c.Value == "Provider");
                if (isProvider)
                {
                    var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    canEdit = !string.IsNullOrEmpty(uid) && offer.ProviderUserId == uid;
                }
            }

            ViewBag.CanEdit = canEdit;
            return View(offer);
        }

        [Authorize(Policy = "ProviderOnly")]
        public async Task<IActionResult> MyOffers()
        {
            var uid = GetUserId();
            var myOffers = await _db.Offers
                .Where(o => o.ProviderUserId == uid)
                .OrderByDescending(o => o.DateTimeUtc)
                .ToListAsync();
            return View(myOffers);
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AllowedCategories = AllowedCategories;
            return View(new Offer { DateTimeUtc = DateTime.UtcNow.AddDays(1) });
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Offer offer)
        {
            if (string.IsNullOrWhiteSpace(offer.Category) || !AllowedCategories.Contains(offer.Category))
                ModelState.AddModelError(nameof(offer.Category), "Catégorie invalide.");

            if (!ModelState.IsValid)
            {
                ViewBag.AllowedCategories = AllowedCategories;
                return View(offer);
            }

            offer.ProviderUserId = GetUserId();
            offer.DateTimeUtc = offer.DateTimeUtc ?? DateTime.UtcNow.AddDays(1);
            _db.Offers.Add(offer);
            await _db.SaveChangesAsync();

            TempData["Msg"] = "Offre publiée avec succès.";
            return RedirectToAction(nameof(MyOffers));
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var offer = await _db.Offers.FindAsync(id);
            if (offer == null || offer.ProviderUserId != GetUserId())
                return Forbid();

            ViewBag.AllowedCategories = AllowedCategories;
            return View(offer);
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Offer offer)
        {
            var existing = await _db.Offers.FindAsync(offer.Id);
            if (existing == null || existing.ProviderUserId != GetUserId())
                return Forbid();

            existing.Title = offer.Title;
            existing.Description = offer.Description;
            existing.Category = offer.Category;
            existing.Price = offer.Price;
            existing.Location = offer.Location;
            existing.ImageUrl = offer.ImageUrl;
            existing.DateTimeUtc = offer.DateTimeUtc ?? existing.DateTimeUtc;

            await _db.SaveChangesAsync();
            TempData["Msg"] = "Offre modifiée avec succès.";
            return RedirectToAction(nameof(MyOffers));
        }

        [Authorize(Policy = "ProviderOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var offer = await _db.Offers.FindAsync(id);
            if (offer == null || offer.ProviderUserId != GetUserId())
                return Forbid();

            _db.Offers.Remove(offer);
            await _db.SaveChangesAsync();
            TempData["Msg"] = "Offre supprimée.";
            return RedirectToAction(nameof(MyOffers));
        }
    }
}
