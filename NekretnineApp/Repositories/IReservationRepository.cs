using NekretnineApp.Models;

namespace NekretnineApp.Repositories
{
    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(int id);
        Task<List<Reservation>> GetByUserIdAsync(string userId);
        Task<List<Reservation>> GetForAdvertiserAsync(string advertiserId, ReservationStatus? status = null);
        Task<List<Reservation>> GetAllAsync(ReservationStatus? status = null);
        Task<List<Reservation>> GetRecentAsync(int count);
        Task<bool> HasConflictAsync(int propertyId, DateTime appointmentDateTime, int? excludeId = null, bool approvedOnly = false);
        Task<int> CountAsync();
        Task<int> CountByStatusAsync(ReservationStatus status);
        Task AddAsync(Reservation reservation);
        void Update(Reservation reservation);
        Task SaveAsync();
    }
}
