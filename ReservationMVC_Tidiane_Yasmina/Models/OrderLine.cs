using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationMVC.Models
{
    public class OrderLine
    {
        [Key] public int Id { get; set; }

        [Required] public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required] public int OfferId { get; set; }
        [ForeignKey(nameof(OfferId))] public Offer? Offer { get; set; }

        [Required] public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
