using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ReservationMVC.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1,
        Cancelled = 2
    }

    public class Order
    {
        [Key] public int Id { get; set; }

        [Required] public string UserId { get; set; } = default!;
        public User? User { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public List<OrderLine> Lines { get; set; } = new();
    }
}
