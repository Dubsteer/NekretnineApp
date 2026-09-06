using Microsoft.EntityFrameworkCore;
using NekretnineApp.Data;
using NekretnineApp.Models;

namespace NekretnineApp.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Category>> GetAllAsync(bool activeOnly = false)
        {
            var query = _context.Categories.AsQueryable();
            if (activeOnly)
                query = query.Where(c => c.IsActive);

            return query.OrderBy(c => c.Name).ToListAsync();
        }

        public Task<Category?> GetByIdAsync(int id)
        {
            return _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId = null)
        {
            var normalized = name.Trim();
            return _context.Categories.AnyAsync(c => c.Name == normalized && (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public Task<bool> HasPropertiesAsync(int id)
        {
            return _context.Properties.AnyAsync(p => p.CategoryId == id);
        }

        public Task<int> CountAsync() => _context.Categories.CountAsync();

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
        }

        public void Update(Category category)
        {
            _context.Categories.Update(category);
        }

        public void Delete(Category category)
        {
            _context.Categories.Remove(category);
        }

        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
