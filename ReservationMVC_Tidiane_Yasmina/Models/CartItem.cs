using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationMVC.Models
{
    public class CartItem
    {
        [Key] public int Id { get; set; }

        [Required] public string UserId { get; set; } = default!;
        public User? User { get; set; }

        [Required] public int OfferId { get; set; }
        public Offer? Offer { get; set; }

        [Range(1, 10)] public int Quantity { get; set; } = 1;

        [NotMapped]
        public decimal TotalPrice => (Offer?.Price ?? 0) * Quantity;
    }
}
