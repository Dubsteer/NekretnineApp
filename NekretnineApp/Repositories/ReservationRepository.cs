using Microsoft.EntityFrameworkCore;
using NekretnineApp.Data;
using NekretnineApp.Models;

namespace NekretnineApp.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<Reservation> WithDetails()
        {
            return _context.Reservations
                .Include(r => r.Property)
                    .ThenInclude(p => p!.Category)
                .Include(r => r.Property)
                    .ThenInclude(p => p!.Owner)
                .Include(r => r.User);
        }

        public Task<Reservation?> GetByIdAsync(int id)
        {
            return WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        }

        public Task<List<Reservation>> GetByUserIdAsync(string userId)
        {
            return WithDetails()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public Task<List<Reservation>> GetForAdvertiserAsync(string advertiserId, ReservationStatus? status = null)
        {
            var query = WithDetails().Where(r => r.Property != null && r.Property.UserId == advertiserId);
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public Task<List<Reservation>> GetAllAsync(ReservationStatus? status = null)
        {
            var query = WithDetails();
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public Task<List<Reservation>> GetRecentAsync(int count)
        {
            return WithDetails()
                .OrderByDescending(r => r.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public Task<bool> HasConflictAsync(
            int propertyId,
            DateTime appointmentDateTime,
            int? excludeId = null,
            bool approvedOnly = false)
        {
            var query = _context.Reservations.Where(r =>
                r.PropertyId == propertyId &&
                r.AppointmentDateTime == appointmentDateTime &&
                (!excludeId.HasValue || r.Id != excludeId.Value));

            query = approvedOnly
                ? query.Where(r => r.Status == ReservationStatus.Approved)
                : query.Where(r => r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Approved);

            return query.AnyAsync();
        }

        public Task<int> CountAsync() => _context.Reservations.CountAsync();

        public Task<int> CountByStatusAsync(ReservationStatus status) =>
            _context.Reservations.CountAsync(r => r.Status == status);

        public async Task AddAsync(Reservation reservation)
        {
            await _context.Reservations.AddAsync(reservation);
        }

        public void Update(Reservation reservation)
        {
            _context.Reservations.Update(reservation);
        }

        public Task SaveAsync() => _context.SaveChangesAsync();
    }
}
