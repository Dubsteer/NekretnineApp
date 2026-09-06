namespace NekretnineApp.Models.ViewModels
{
    public class ReservationItemViewModel
    {
        public Reservation Reservation { get; set; } = new();
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
    }
}
