namespace NekretnineApp.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAdvertisers { get; set; }
        public int TotalRegularUsers { get; set; }
        public int TotalCategories { get; set; }
        public int TotalProperties { get; set; }
        public int ActiveProperties { get; set; }
        public int InactiveProperties { get; set; }
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
        public int ApprovedReservations { get; set; }
        public List<ReservationItemViewModel> RecentReservations { get; set; } = new();
    }
}
