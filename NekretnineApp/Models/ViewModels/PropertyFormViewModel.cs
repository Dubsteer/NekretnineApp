using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace NekretnineApp.Models.ViewModels
{
    public class PropertyFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naziv oglasa je obavezan.")]
        [StringLength(100)]
        [Display(Name = "Naziv oglasa")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opis nekretnine je obavezan.")]
        [StringLength(1500)]
        [Display(Name = "Opis nekretnine")]
        public string Description { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Izaberite kategoriju.")]
        [Display(Name = "Tip nekretnine")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Namjena je obavezna.")]
        [Display(Name = "Namjena")]
        public string Purpose { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grad je obavezan.")]
        [StringLength(100)]
        [Display(Name = "Grad")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adresa je obavezna.")]
        [StringLength(200)]
        [Display(Name = "Adresa")]
        public string Address { get; set; } = string.Empty;

        [Range(1, 1000000, ErrorMessage = "Površina mora biti veća od nule.")]
        [Display(Name = "Površina (m²)")]
        public double Area { get; set; }

        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Cijena mora biti veća od nule.")]
        [Display(Name = "Cijena (€)")]
        public decimal Price { get; set; }

        [Display(Name = "Fotografije")]
        public List<IFormFile> Photos { get; set; } = new();

        public List<PropertyImage> ExistingImages { get; set; } = new();
        public List<int> RemoveImageIds { get; set; } = new();
    }
}
