using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;

namespace NekretnineApp.Repositories
{
    public interface IPropertyRepository
    {
        Task<List<Property>> GetFilteredAsync(PropertyFilter filter, bool includeInactive);
        Task<List<Property>> GetByUserIdAsync(string userId);
        Task<Property?> GetByIdAsync(int id);
        Task<List<string>> GetCitiesAsync();
        Task<PropertyImage?> GetImageByIdAsync(int imageId);
        Task<int> CountAsync();
        Task<int> CountActiveAsync();
        Task<bool> AnyByUserIdAsync(string userId);
        Task AddAsync(Property property);
        void Update(Property property);
        void Delete(Property property);
        void DeleteImage(PropertyImage image);
        Task SaveAsync();
    }
}
