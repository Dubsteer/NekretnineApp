using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace NekretnineApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }
    }
}
