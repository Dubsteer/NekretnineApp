using Microsoft.EntityFrameworkCore;
using NekretnineApp.Data;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;

namespace NekretnineApp.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly ApplicationDbContext _context;

        public PropertyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Property>> GetFilteredAsync(PropertyFilter filter, bool includeInactive)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Owner)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Purpose))
                query = query.Where(p => p.Purpose == filter.Purpose);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(p => p.City == filter.City);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.MinArea.HasValue)
                query = query.Where(p => p.Area >= filter.MinArea.Value);

            if (filter.MaxArea.HasValue)
                query = query.Where(p => p.Area <= filter.MaxArea.Value);

            return await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<List<Property>> GetByUserIdAsync(string userId)
        {
            return _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public Task<Property?> GetByIdAsync(int id)
        {
            return _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Owner)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<List<string>> GetCitiesAsync()
        {
            return _context.Properties
                .Where(p => p.IsActive)
                .Select(p => p.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public Task<PropertyImage?> GetImageByIdAsync(int imageId)
        {
            return _context.PropertyImages.FirstOrDefaultAsync(i => i.Id == imageId);
        }

        public Task<int> CountAsync() => _context.Properties.CountAsync();

        public Task<int> CountActiveAsync() => _context.Properties.CountAsync(p => p.IsActive);

        public Task<bool> AnyByUserIdAsync(string userId) =>
            _context.Properties.AnyAsync(p => p.UserId == userId);

        public async Task AddAsync(Property property)
        {
            await _context.Properties.AddAsync(property);
        }

        public void Update(Property property)
        {
            _context.Properties.Update(property);
        }

        public void Delete(Property property)
        {
            _context.Properties.Remove(property);
        }

        public void DeleteImage(PropertyImage image)
        {
            _context.PropertyImages.Remove(image);
        }

        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
