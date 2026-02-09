using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace ReservationMVC.Controllers
{
    [Authorize(Policy = "ClientOnly")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        public OrdersController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;

            var key = _config["Stripe:SecretKey"];
            if (!string.IsNullOrWhiteSpace(key))
            {
                if (key.StartsWith("sk_test_") || key.StartsWith("sk_live_"))
                    StripeConfiguration.ApiKey = key;
            }
        }

        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();

            var items = await _db.CartItems
                .Include(c => c.Offer)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!items.Any())
            {
                TempData["Msg"] = "Votre panier est vide.";
                return RedirectToAction("Index", "Cart");
            }

            var domain = $"{Request.Scheme}://{Request.Host}";

            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + Url.Action("Success", "Orders")!,
                CancelUrl = domain + Url.Action("Index", "Cart")!,
                Mode = "payment",
                LineItems = items.Select(i => new SessionLineItemOptions
                {
                    Quantity = i.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(i.Offer!.Price * 100m),
                        Currency = "cad",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.Offer.Title
                        }
                    }
                }).ToList()
            };

            try
            {
                var service = new SessionService();
                var session = service.Create(options);

                if (string.IsNullOrWhiteSpace(session?.Url))
                {
                    TempData["Err"] = "La plateforme de paiement n’a pas renvoyé d’URL de redirection.";
                    return RedirectToAction("Index", "Cart");
                }

                return Redirect(session.Url!);
            }
            catch (StripeException sx)
            {
                TempData["Err"] = $"Paiement indisponible : {sx.Message}";
                return RedirectToAction("Index", "Cart");
            }
            catch (Exception)
            {
                TempData["Err"] = "Une erreur est survenue lors de la création du paiement.";
                return RedirectToAction("Index", "Cart");
            }
        }

        public async Task<IActionResult> Success()
        {
            var userId = GetUserId();

            var items = await _db.CartItems
                .Include(c => c.Offer)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (items.Any())
            {
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = items.Sum(i => i.Offer!.Price * i.Quantity),
                    Status = OrderStatus.Paid
                };

                order.Lines = items.Select(i => new OrderLine
                {
                    OfferId = i.OfferId,
                    Quantity = i.Quantity,
                    UnitPrice = i.Offer!.Price
                }).ToList();

                _db.Orders.Add(order);

                var invoice = new Models.Invoice
                {
                    Order = order,
                    BuyerUserId = userId,
                    Amount = order.TotalAmount
                };
                _db.Invoices.Add(invoice);

                _db.CartItems.RemoveRange(items);
                await _db.SaveChangesAsync();
            }

            return View();
        }
    }
}
