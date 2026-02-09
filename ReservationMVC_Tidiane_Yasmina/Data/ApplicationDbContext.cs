using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReservationMVC.Models;

namespace ReservationMVC.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Offer> Offers { get; set; } = default!;
        public DbSet<CartItem> CartItems { get; set; } = default!;
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderLine> OrderLines { get; set; } = default!;
        public DbSet<Invoice> Invoices { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString =
                    "Data Source=(localdb)\\MSSQLLocalDB;" +
                    "Initial Catalog=ReservationDb;" +
                    "Integrated Security=True;" +
                    "Encrypt=False;TrustServerCertificate=True;";
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Offer>()
                .Property(o => o.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.UserId, ci.OfferId })
                .IsUnique();

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Lines)
                .WithOne(l => l.Order)
                .HasForeignKey(l => l.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderLine>()
                .Property(ol => ol.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Number)
                .HasMaxLength(64);

            modelBuilder.Entity<Offer>().HasData(
                new Offer
                {
                    Id = 1,
                    Title = "Billet Cinéma - Action",
                    Category = "Cinéma",
                    Location = "Salle A",
                    Price = 14.99m,
                    ImageUrl = "https://picsum.photos/seed/action/600/400",
                    DateTimeUtc = new DateTime(2025, 01, 01, 18, 00, 00, DateTimeKind.Utc)
                },
                new Offer
                {
                    Id = 2,
                    Title = "Billet Cinéma - Comédie",
                    Category = "Cinéma",
                    Location = "Salle B",
                    Price = 12.99m,
                    ImageUrl = "https://picsum.photos/seed/comedie/600/400",
                    DateTimeUtc = new DateTime(2025, 01, 02, 18, 00, 00, DateTimeKind.Utc)
                },
                new Offer
                {
                    Id = 3,
                    Title = "Salle de sport - Créneau 18h",
                    Category = "Sport",
                    Location = "Gym Centre",
                    Price = 9.99m,
                    ImageUrl = "https://picsum.photos/seed/sport/600/400",
                    DateTimeUtc = new DateTime(2025, 01, 03, 18, 00, 00, DateTimeKind.Utc)
                }
            );
        }
    }
}
