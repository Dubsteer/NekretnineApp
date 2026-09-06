using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace NekretnineApp.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [ValidateNever]
        public Property? Property { get; set; }

        [Required(ErrorMessage = "Datum i vrijeme obilaska su obavezni.")]
        [Display(Name = "Datum i vrijeme obilaska")]
        public DateTime AppointmentDateTime { get; set; }

        [StringLength(500, ErrorMessage = "Napomena može imati najviše 500 karaktera.")]
        [Display(Name = "Napomena")]
        public string? Note { get; set; }

        public string UserId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? User { get; set; }

        [Display(Name = "Status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Display(Name = "Kreirano")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
