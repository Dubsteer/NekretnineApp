using NekretnineApp.Models;

namespace NekretnineApp.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(bool activeOnly = false);
        Task<Category?> GetByIdAsync(int id);
        Task<bool> NameExistsAsync(string name, int? excludeId = null);
        Task<bool> HasPropertiesAsync(int id);
        Task<int> CountAsync();
        Task AddAsync(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task SaveAsync();
    }
}
