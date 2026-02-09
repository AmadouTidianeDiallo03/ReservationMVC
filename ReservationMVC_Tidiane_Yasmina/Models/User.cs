using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ReservationMVC.Models
{
    public enum AppRole { Client = 0, Provider = 1 }

    public class User : IdentityUser
    {
        [MaxLength(120)] public string? FullName { get; set; }
        public AppRole Role { get; set; } = AppRole.Client;

        [MaxLength(240)] public string? Address { get; set; }
        [MaxLength(120)] public string? City { get; set; }
        [MaxLength(20)] public string? PostalCode { get; set; }
        [MaxLength(60)] public string? Country { get; set; }
    }
}
