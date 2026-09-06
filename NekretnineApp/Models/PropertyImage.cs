using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NekretnineApp.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }

        [ValidateNever]
        public Property? Property { get; set; }

        [Required]
        [StringLength(300)]
        public string ImagePath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
