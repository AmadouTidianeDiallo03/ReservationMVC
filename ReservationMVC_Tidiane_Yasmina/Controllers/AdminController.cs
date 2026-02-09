using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;
using ReservationMVC.Services;

namespace ReservationMVC.Controllers
{
    [Authorize(Policy = "ProviderOnly")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly DataImportService? _import;

        public AdminController(ApplicationDbContext db, DataImportService? import = null)
        {
            _db = db;
            _import = import;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            var paidOrders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Paid)
                .Include(o => o.Lines)
                .ToListAsync();

            var totalRevenue = paidOrders.Sum(o => o.TotalAmount);

            var todayStart = DateTime.UtcNow.Date;
            var tomorrow = todayStart.AddDays(1);

            var todayRevenue = await _db.Orders
                .Where(o => o.Status == OrderStatus.Paid &&
                            o.CreatedUtc >= todayStart && o.CreatedUtc < tomorrow)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var topCustomersRaw = await _db.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Paid)
                .GroupBy(o => o.UserId!)
                .Select(g => new { UserId = g.Key, Amount = g.Sum(o => o.TotalAmount) })
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToListAsync();

            var userIds = topCustomersRaw.Select(t => t.UserId).ToList();
            var usersMap = await _db.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? u.Id);

            var topOffersRaw = await _db.OrderLines
                .AsNoTracking()
                .Where(l => l.Order.Status == OrderStatus.Paid)
                .GroupBy(l => l.OfferId)
                .Select(g => new
                {
                    OfferId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            var offerIds = topOffersRaw.Select(t => t.OfferId).ToList();
            var offerTitleMap = await _db.Offers
                .Where(o => offerIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.Title);

            decimal myRevenue = 0m;
            int myOrders = 0;
            if (CurrentUserId != null)
            {
                var myLines = await _db.OrderLines
                    .AsNoTracking()
                    .Include(l => l.Order)
                    .Include(l => l.Offer)
                    .Where(l => l.Order.Status == OrderStatus.Paid &&
                                l.Offer!.ProviderUserId == CurrentUserId)
                    .ToListAsync();

                myRevenue = myLines.Sum(l => l.UnitPrice * l.Quantity);
                myOrders = myLines.Select(l => l.OrderId).Distinct().Count();
            }

            double? globalFill = null;
            var fillRates = new List<(string OfferTitle, double Rate)>();

            var capacityOffers = await _db.Offers
                .AsNoTracking()
                .Where(o => o.Capacity != null)
                .ToListAsync();

            if (capacityOffers.Any())
            {
                var qtyByOffer = await _db.OrderLines
                    .AsNoTracking()
                    .Where(l => l.Order.Status == OrderStatus.Paid)
                    .GroupBy(l => l.OfferId)
                    .Select(g => new { OfferId = g.Key, Qty = g.Sum(x => x.Quantity) })
                    .ToDictionaryAsync(x => x.OfferId, x => x.Qty);

                foreach (var off in capacityOffers)
                {
                    qtyByOffer.TryGetValue(off.Id, out var qty);
                    var cap = off.Capacity!.Value;
                    var rate = cap > 0 ? Math.Min(1.0, (double)qty / cap) : 0.0;
                    fillRates.Add((off.Title, rate));
                }

                globalFill = fillRates.Any() ? fillRates.Average(x => x.Rate) : 0.0;
            }

            var vm = new AdminDashboardVm
            {
                OrdersCount = await _db.Orders.CountAsync(),
                PaidOrdersCount = paidOrders.Count,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,

                TopCustomers = topCustomersRaw
                    .Select(t => (usersMap.TryGetValue(t.UserId, out var email) ? email : t.UserId, t.Amount))
                    .ToList(),

                TopOffers = topOffersRaw
                    .Select(t => (offerTitleMap.TryGetValue(t.OfferId, out var title) ? title : $"Offre #{t.OfferId}",
                                  t.Quantity, t.Revenue))
                    .ToList(),

                GlobalFillRate = globalFill,
                FillRates = fillRates,

                MyRevenue = myRevenue,
                MyOrders = myOrders,

                OffersCount = await _db.Offers.CountAsync(),
                UsersCount = await _db.Users.CountAsync()
            };

            return View("Dashboard", vm);
        }

        public async Task<IActionResult> Orders()
        {
            var data = await _db.Orders
                .AsNoTracking()
                .Include(o => o.Lines)
                .OrderByDescending(o => o.CreatedUtc)
                .Take(200)
                .ToListAsync();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportDemo([FromServices] DataImportService import)
        {
            await import.CleanupNonReservationOffersAsync();
            await import.ImportReservationOffersAsync(24);
            await import.ImportDummyJsonUsersAsync(10);

            TempData["Msg"] = "Import de données de réservation effectué.";
            return RedirectToAction(nameof(Index));
        }


    }
}
