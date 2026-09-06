using System.ComponentModel.DataAnnotations;

namespace NekretnineApp.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naziv kategorije je obavezan.")]
        [StringLength(100)]
        [Display(Name = "Naziv kategorije")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Aktivna")]
        public bool IsActive { get; set; } = true;

        public ICollection<Property> Properties { get; set; } = new List<Property>();
    }
}
