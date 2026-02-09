namespace ReservationMVC.Models
{
    public class InvoiceDetailsVm
    {
        public Invoice Invoice { get; set; } = default!;
        public Order? Order { get; set; }
        public List<OrderLine> Lines { get; set; } = new();
        public string BuyerDisplay { get; set; } = "";   // Nom/Email du client
    }
}
