
using System;

namespace ReservationMVC.Models
{
    public class OfferFilter
    {
        public string? Q { get; set; }                
        public string? Category { get; set; }          
        public DateTime? DateMin { get; set; }
        public DateTime? DateMax { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }
        public string? Sort { get; set; }              
        public bool FutureOnly { get; set; } = true;  

        
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
