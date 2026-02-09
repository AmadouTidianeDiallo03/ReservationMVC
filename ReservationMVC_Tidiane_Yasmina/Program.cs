using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Data;
using ReservationMVC.Models;
using ReservationMVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException(
        "La chaine de connexion 'DefaultConnection' est absente. " +
        "Ajoute-la dans appsettings.json -> ConnectionStrings.DefaultConnection.");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(cs));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProviderOnly", p => p.RequireClaim("app:role", "Provider"));
    options.AddPolicy("ClientOnly", p => p.RequireClaim("app:role", "Client"));
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<DataImportService>();

var stripeSecret = builder.Configuration["Stripe:SecretKey"];
if (string.IsNullOrWhiteSpace(stripeSecret))
    throw new InvalidOperationException("Stripe:SecretKey manquante.");
if (!(stripeSecret.StartsWith("sk_test_") || stripeSecret.StartsWith("sk_live_")))
    throw new InvalidOperationException("Stripe:SecretKey invalide : doit commencer par 'sk_test_' ou 'sk_live_'.");
Stripe.StripeConfiguration.ApiKey = stripeSecret;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;

    try
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Offers.AnyAsync())
        {
            db.Offers.AddRange(
                new Offer
                {
                    Title = "Billet Cinema - Action",
                    Category = "Cinema",
                    Location = "Salle A",
                    Price = 14.99m,
                    ImageUrl = "https://picsum.photos/seed/action/600/400",
                    DateTimeUtc = DateTime.UtcNow.AddDays(1)
                },
                new Offer
                {
                    Title = "Billet Cinema - Comedie",
                    Category = "Cinema",
                    Location = "Salle B",
                    Price = 12.99m,
                    ImageUrl = "https://picsum.photos/seed/comedie/600/400",
                    DateTimeUtc = DateTime.UtcNow.AddDays(2)
                },
                new Offer
                {
                    Title = "Creneau Salle de sport 18h",
                    Category = "Sport",
                    Location = "Gym Centre",
                    Price = 9.99m,
                    ImageUrl = "https://picsum.photos/seed/sport/600/400",
                    DateTimeUtc = DateTime.UtcNow.AddDays(3)
                }
            );
            await db.SaveChangesAsync();
        }

        var importer = sp.GetRequiredService<DataImportService>();
        try
        {
            await importer.CleanupNonReservationOffersAsync();
            await importer.ImportReservationOffersAsync(24);
            await importer.ImportDummyJsonUsersAsync(10);
        }
        catch (Exception netEx)
        {
            Console.Error.WriteLine("Import ignore (reseau/format) : " + netEx.Message);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Erreur de migration/seed : " + ex);
        throw;
    }
}

app.Run();
