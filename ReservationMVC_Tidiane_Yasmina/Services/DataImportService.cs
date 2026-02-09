using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;

namespace ReservationMVC.Services
{
    public class DataImportService
    {
        private static readonly string[] CinemaGenres =
            { "Action", "Comédie", "Drame", "Aventure", "Sci-Fi", "Animation" };
        private static readonly string[] CinemaRooms = { "Salle A", "Salle B", "IMAX 1", "VIP 2" };

        private static readonly string[] Sports =
            { "Football", "Basket", "Tennis", "Badminton", "CrossFit", "Natation" };
        private static readonly string[] Gyms = { "Gym Centre", "Sport+ Montcalm", "Stade Local", "Complexe 2000" };

        private static readonly string[] Cities =
            { "Montréal", "Québec", "Laval", "Sherbrooke", "Trois-Rivières", "Gatineau" };
        private static readonly string[] Hotels = { "Hôtel Horizon", "Auberge du Parc", "Palace Nord", "Lodge Downtown" };

        private static readonly string[] Leisure =
            { "Escape game", "Karaoké", "Patinage", "Bowling", "Mini-golf", "Accrobranche" };
        private static readonly string[] LeisureSites = { "Loisirs Center", "Fun Park", "Aqualand", "Parc Aventure" };

        private static readonly string[] ImgCinema = {
            "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?q=80&w=1200",
            "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?q=80&w=1200"
        };
        private static readonly string[] ImgSport = {
            "https://images.unsplash.com/photo-1517649763962-0c623066013b?q=80&w=1200",
            "https://images.unsplash.com/photo-1546519638-68e109498ffc?q=80&w=1200"
        };
        private static readonly string[] ImgHotel = {
            "https://images.unsplash.com/photo-1501117716987-c8e2a7a0f1c5?q=80&w=1200",
            "https://images.unsplash.com/photo-1551776235-dde6d4829808?q=80&w=1200"
        };
        private static readonly string[] ImgLeisure = {
            "https://images.unsplash.com/photo-1508606572321-901ea443707f?q=80&w=1200",
            "https://images.unsplash.com/photo-1512428559087-560fa5ceab42?q=80&w=1200"
        };

        private readonly HttpClient _http;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public DataImportService(HttpClient http, ApplicationDbContext db, UserManager<User> userManager)
        {
            _http = http;
            _db = db;
            _userManager = userManager;
        }

        public async Task CleanupNonReservationOffersAsync()
        {
            var keep = new[] { "Cinéma", "Sport", "Hôtel", "Loisirs" };
            var toDelete = await _db.Offers.Where(o => !keep.Contains(o.Category)).ToListAsync();
            if (toDelete.Count > 0)
            {
                _db.Offers.RemoveRange(toDelete);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ImportReservationOffersAsync(int targetCount = 24)
        {
            var fake = await SafeGet<List<FakeStoreProductDto>>("https://fakestoreapi.com/products") ?? new();
            var dummy = await SafeGet<DummyProductsResponse>("https://dummyjson.com/products?limit=50") ?? new();
            var pool = new List<(string title, double price)>();

            pool.AddRange(fake.Select(p => (p.Title ?? "Produit", p.Price)));
            pool.AddRange((dummy.Products ?? new())
                           .Select(p => (p.Title ?? p.Brand ?? "Item", p.Price)));

            if (pool.Count == 0) return;

            var rnd = Random.Shared;
            var now = DateTime.UtcNow;

            foreach (var seed in pool.Take(targetCount))
            {
                var pick = rnd.Next(4);
                Offer offer;

                switch (pick)
                {
                    case 0:
                        var genre = CinemaGenres[rnd.Next(CinemaGenres.Length)];
                        var room = CinemaRooms[rnd.Next(CinemaRooms.Length)];
                        offer = new Offer
                        {
                            Title = $"Billet Cinéma - {genre}",
                            Category = "Cinéma",
                            Location = room,
                            Description = $"Séance {genre}. {seed.title}",
                            Price = (decimal)Math.Max(8, Math.Round(seed.price, 2)),
                            ImageUrl = ImgCinema[rnd.Next(ImgCinema.Length)],
                            DateTimeUtc = now.AddDays(rnd.Next(1, 14)).Date.AddHours(13 + rnd.Next(0, 9))
                        };
                        break;

                    case 1:
                        var sp = Sports[rnd.Next(Sports.Length)];
                        var gym = Gyms[rnd.Next(Gyms.Length)];
                        offer = new Offer
                        {
                            Title = $"Créneau {sp} {rnd.Next(8, 22)}h",
                            Category = "Sport",
                            Location = gym,
                            Description = $"Réservez un créneau {sp}. {seed.title}",
                            Price = (decimal)Math.Max(7, Math.Round(seed.price / 2, 2)),
                            ImageUrl = ImgSport[rnd.Next(ImgSport.Length)],
                            DateTimeUtc = now.AddDays(rnd.Next(1, 10)).Date.AddHours(rnd.Next(7, 21))
                        };
                        break;

                    case 2:
                        var city = Cities[rnd.Next(Cities.Length)];
                        var hotel = Hotels[rnd.Next(Hotels.Length)];
                        offer = new Offer
                        {
                            Title = $"Nuit d’hôtel – {city}",
                            Category = "Hôtel",
                            Location = hotel,
                            Description = $"Chambre double à {city}. {seed.title}",
                            Price = (decimal)Math.Max(59, Math.Round(seed.price * 8, 2)),
                            ImageUrl = ImgHotel[rnd.Next(ImgHotel.Length)],
                            DateTimeUtc = now.AddDays(rnd.Next(1, 30)).Date.AddHours(15)
                        };
                        break;

                    default:
                        var act = Leisure[rnd.Next(Leisure.Length)];
                        var site = LeisureSites[rnd.Next(LeisureSites.Length)];
                        offer = new Offer
                        {
                            Title = $"Activité {act}",
                            Category = "Loisirs",
                            Location = site,
                            Description = $"{act} – {seed.title}",
                            Price = (decimal)Math.Max(9, Math.Round(seed.price / 1.5, 2)),
                            ImageUrl = ImgLeisure[rnd.Next(ImgLeisure.Length)],
                            DateTimeUtc = now.AddDays(rnd.Next(1, 21)).Date.AddHours(rnd.Next(10, 20))
                        };
                        break;
                }

                var exists = await _db.Offers.AnyAsync(o =>
                    o.Title == offer.Title && o.DateTimeUtc == offer.DateTimeUtc);
                if (!exists) _db.Offers.Add(offer);
            }

            await _db.SaveChangesAsync();
        }

        public async Task ImportDummyJsonUsersAsync(int max = 10)
        {
            var res = await SafeGet<DummyUsersResponse>("https://dummyjson.com/users?limit=50")
                      ?? new DummyUsersResponse();
            var take = (res.Users ?? new()).Where(u => !string.IsNullOrWhiteSpace(u.Email)).Take(max);

            foreach (var u in take)
            {
                var email = u.Email!.Trim().ToLowerInvariant();
                if (await _userManager.FindByEmailAsync(email) is not null) continue;

                var user = new User
                {
                    UserName = email,
                    Email = email,
                    FullName = $"{u.FirstName} {u.LastName}".Trim(),
                    Role = AppRole.Client
                };

                var result = await _userManager.CreateAsync(user, "Demo@1234");
                if (result.Succeeded)
                    await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("app:role", "Client"));
            }
        }

        private async Task<T?> SafeGet<T>(string url)
        {
            try { return await _http.GetFromJsonAsync<T>(url); }
            catch { return default; }
        }
    }

    public class FakeStoreProductDto
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("price")] public double Price { get; set; }
    }

    public class DummyProductsResponse
    {
        [JsonPropertyName("products")] public List<DummyProduct>? Products { get; set; } = new();
    }
    public class DummyProduct
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("brand")] public string? Brand { get; set; }
        [JsonPropertyName("price")] public double Price { get; set; }
    }

    public class DummyUsersResponse
    {
        [JsonPropertyName("users")] public List<DummyUser>? Users { get; set; } = new();
    }
    public class DummyUser
    {
        [JsonPropertyName("firstName")] public string? FirstName { get; set; }
        [JsonPropertyName("lastName")] public string? LastName { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
    }
}
