namespace NekretnineApp.Models
{
    public enum ReservationStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3
    }

    public static class ReservationStatusExtensions
    {
        public static string DisplayName(this ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Na čekanju",
                ReservationStatus.Approved => "Prihvaćeno",
                ReservationStatus.Rejected => "Odbijeno",
                ReservationStatus.Cancelled => "Otkazano",
                _ => status.ToString()
            };
        }
    }
}
