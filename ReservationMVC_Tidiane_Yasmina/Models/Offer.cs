using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationMVC.Models
{
    public class Offer
    {
        public int Id { get; set; }

        [Required, MaxLength(180)]
        public string Title { get; set; } = default!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, 999999)]
        [Precision(18, 2)] 
        public decimal Price { get; set; }

        [MaxLength(120)]
        public string? Category { get; set; }   // ex: Cinéma, Sport, Hôtel

        [MaxLength(120)]
        public string? Location { get; set; }   

        public DateTime? DateTimeUtc { get; set; } 

        public string? ImageUrl { get; set; }

       
        public int? Capacity { get; set; }
        public int? Booked { get; set; } = 0;

       
        public string? ProviderUserId { get; set; }
    }
}
