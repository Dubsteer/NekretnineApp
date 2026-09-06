namespace NekretnineApp.Models.ViewModels
{
    public class ReportsViewModel
    {
        public List<CityReservationReportItem> ByCity { get; set; } = new();
        public List<PropertyReservationReportItem> ByProperty { get; set; } = new();
        public List<MonthlyReservationReportItem> ByMonth { get; set; } = new();
    }

    public class CityReservationReportItem
    {
        public string City { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PropertyReservationReportItem
    {
        public string PropertyTitle { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyReservationReportItem
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
