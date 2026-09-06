namespace NekretnineApp.Models.ViewModels
{
    public class PropertyFilter
    {
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? Purpose { get; set; }
        public string? City { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinArea { get; set; }
        public double? MaxArea { get; set; }
    }
}
