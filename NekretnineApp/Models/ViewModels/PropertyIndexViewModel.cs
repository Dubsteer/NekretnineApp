namespace NekretnineApp.Models.ViewModels
{
    public class PropertyIndexViewModel
    {
        public PropertyFilter Filter { get; set; } = new();
        public List<Property> Properties { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<string> Cities { get; set; } = new();
    }
}
