namespace ReservationMVC.Models
{
    public class AdminDashboardVm
    {

        public int OrdersCount { get; set; }
        public int PaidOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }

        public List<(string CustomerEmail, decimal Amount)> TopCustomers { get; set; } = new();
        public List<(string OfferTitle, int Quantity, decimal Revenue)> TopOffers { get; set; } = new();

        public double? GlobalFillRate { get; set; }
        public List<(string OfferTitle, double Rate)> FillRates { get; set; } = new();

        public decimal MyRevenue { get; set; }
        public int MyOrders { get; set; }

        // Nouveaux compteurs à afficher
        public int OffersCount { get; set; }
        public int UsersCount { get; set; }
    }
}
