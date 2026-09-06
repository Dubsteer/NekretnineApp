using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;
using NekretnineApp.Repositories;

namespace NekretnineApp.Tests;

[TestClass]
public class PropertyRepositoryTests
{
    [TestMethod]
    public async Task GetFilteredAsync_ReturnsOnlyActiveMatchingProperties()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = new Category { Name = "Stan" };
        var owner = new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            FullName = "Demo Owner"
        };

        database.Context.AddRange(category, owner);
        await database.Context.SaveChangesAsync();

        database.Context.Properties.AddRange(
            CreateProperty("Centar Podgorice", "Podgorica", 120_000m, true, category.Id, owner.Id),
            CreateProperty("Van grada", "Nikšić", 80_000m, true, category.Id, owner.Id),
            CreateProperty("Neaktivan oglas", "Podgorica", 100_000m, false, category.Id, owner.Id));
        await database.Context.SaveChangesAsync();

        var repository = new PropertyRepository(database.Context);
        var result = await repository.GetFilteredAsync(
            new PropertyFilter { City = "Podgorica", MinPrice = 110_000m },
            includeInactive: false);

        Assert.HasCount(1, result);
        Assert.AreEqual("Centar Podgorice", result[0].Title);
    }

    [TestMethod]
    public async Task GetCitiesAsync_ReturnsDistinctActiveCitiesInOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var category = new Category { Name = "Kuća" };
        var owner = new ApplicationUser { Id = "owner-2", FullName = "Demo Owner" };
        database.Context.AddRange(category, owner);
        await database.Context.SaveChangesAsync();

        database.Context.Properties.AddRange(
            CreateProperty("A", "Podgorica", 1m, true, category.Id, owner.Id),
            CreateProperty("B", "Bar", 1m, true, category.Id, owner.Id),
            CreateProperty("C", "Bar", 1m, true, category.Id, owner.Id),
            CreateProperty("D", "Cetinje", 1m, false, category.Id, owner.Id));
        await database.Context.SaveChangesAsync();

        var repository = new PropertyRepository(database.Context);
        var result = await repository.GetCitiesAsync();

        CollectionAssert.AreEqual(new[] { "Bar", "Podgorica" }, result);
    }

    private static Property CreateProperty(
        string title,
        string city,
        decimal price,
        bool isActive,
        int categoryId,
        string ownerId)
    {
        return new Property
        {
            Title = title,
            Description = "Opis nekretnine",
            CategoryId = categoryId,
            Purpose = "Prodaja",
            City = city,
            Address = "Test adresa 1",
            Area = 50,
            Price = price,
            IsActive = isActive,
            UserId = ownerId
        };
    }
}
