using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationMVC.Models
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }
        [Required]
        public string BuyerUserId { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(64)]
        public string Number { get; set; } = Guid.NewGuid().ToString("N")[..10];
    }
}
