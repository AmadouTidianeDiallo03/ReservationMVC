using System.ComponentModel.DataAnnotations;

namespace ReservationMVC.Models
{
    public class ProfileEditVm
    {
        [Required, StringLength(100)]
        [Display(Name = "Nom complet")]
        public string FullName { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Adresse e-mail")]
        public string Email { get; set; } = "";
    }
}
