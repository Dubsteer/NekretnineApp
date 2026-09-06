using System.ComponentModel.DataAnnotations;

namespace NekretnineApp.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ime i prezime ili naziv su obavezni.")]
        [StringLength(100)]
        [Display(Name = "Ime i prezime / naziv")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Naziv firme")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Broj telefona je obavezan.")]
        [Phone(ErrorMessage = "Unesite ispravan broj telefona.")]
        [Display(Name = "Telefon")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Adresa")]
        public string? Address { get; set; }

        public string Role { get; set; } = string.Empty;
    }
}
