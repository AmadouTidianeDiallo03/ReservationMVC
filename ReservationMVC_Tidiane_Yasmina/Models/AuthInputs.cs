using System.ComponentModel.DataAnnotations;

namespace ReservationMVC.Models
{
    public class RegisterInput
    {
        [Required] public string FullName { get; set; } = default!;
        [Required, EmailAddress] public string Email { get; set; } = default!;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = default!;
        [Required] public AppRole Role { get; set; } = AppRole.Client;
    }

    public class LoginInput
    {
        [Required, EmailAddress] public string Email { get; set; } = default!;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = default!;
    }

    public class ProfileEditInput
    {
        [Required] public string FullName { get; set; } = default!;
        [EmailAddress] public string? Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
