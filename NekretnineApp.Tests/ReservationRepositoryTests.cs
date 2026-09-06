using NekretnineApp.Models;
using NekretnineApp.Repositories;

namespace NekretnineApp.Tests;

[TestClass]
public class ReservationRepositoryTests
{
    [TestMethod]
    public async Task HasConflictAsync_TreatsPendingAndApprovedAsConflicts()
    {
        await using var database = await CreateDatabaseWithPropertyAsync();
        var appointment = DateTime.UtcNow.AddDays(2);
        database.Context.Reservations.Add(new Reservation
        {
            PropertyId = 1,
            UserId = "visitor-1",
            AppointmentDateTime = appointment,
            Status = ReservationStatus.Pending
        });
        await database.Context.SaveChangesAsync();

        var repository = new ReservationRepository(database.Context);

        Assert.IsTrue(await repository.HasConflictAsync(1, appointment));
        Assert.IsFalse(await repository.HasConflictAsync(1, appointment.AddMinutes(15)));
    }

    [TestMethod]
    public async Task HasConflictAsync_IgnoresRejectedReservations()
    {
        await using var database = await CreateDatabaseWithPropertyAsync();
        var appointment = DateTime.UtcNow.AddDays(2);
        database.Context.Reservations.Add(new Reservation
        {
            PropertyId = 1,
            UserId = "visitor-1",
            AppointmentDateTime = appointment,
            Status = ReservationStatus.Rejected
        });
        await database.Context.SaveChangesAsync();

        var repository = new ReservationRepository(database.Context);

        Assert.IsFalse(await repository.HasConflictAsync(1, appointment));
    }

    private static async Task<TestDatabase> CreateDatabaseWithPropertyAsync()
    {
        var database = await TestDatabase.CreateAsync();
        var category = new Category { Id = 1, Name = "Stan" };
        var owner = new ApplicationUser { Id = "owner-1", FullName = "Owner" };
        var visitor = new ApplicationUser { Id = "visitor-1", FullName = "Visitor" };
        database.Context.AddRange(category, owner, visitor);
        database.Context.Properties.Add(new Property
        {
            Id = 1,
            Title = "Test property",
            Description = "Test description",
            Category = category,
            Purpose = "Prodaja",
            City = "Podgorica",
            Address = "Test address",
            Area = 50,
            Price = 100_000m,
            UserId = owner.Id
        });
        await database.Context.SaveChangesAsync();
        return database;
    }
}
