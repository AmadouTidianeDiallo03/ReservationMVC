
namespace ReservationMVC.Models
{
    public class ProfilePageModel
    {
        public string? FullName { get; set; }
        public string Email { get; set; } = default!;
        public string Role { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();

        
    }
}
