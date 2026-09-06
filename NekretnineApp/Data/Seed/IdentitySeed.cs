using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NekretnineApp.Models;

namespace NekretnineApp.Data.Seed
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IHostEnvironment environment,
            ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var role in new[] { RoleNames.Admin, RoleNames.Advertiser, RoleNames.User })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException($"Nije moguće kreirati ulogu {role}.");
                }
            }

            if (!await context.Categories.AnyAsync())
            {
                context.Categories.AddRange(
                    new Category { Name = "Stan", IsActive = true },
                    new Category { Name = "Kuća", IsActive = true },
                    new Category { Name = "Poslovni prostor", IsActive = true },
                    new Category { Name = "Zemljište", IsActive = true });
                await context.SaveChangesAsync();
            }

            if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DemoSeed:Enabled"))
                return;

            var demoPassword = configuration["DemoSeed:Password"];
            if (!IsStrongDemoPassword(demoPassword))
            {
                logger.LogWarning(
                    "Demo data was not seeded. Set DemoSeed:Password to a local-only password with at least 12 characters, uppercase, lowercase, a number and a symbol.");
                return;
            }

            await EnsureUserAsync(
                userManager,
                "admin@example.com",
                demoPassword!,
                "Demo administrator",
                "+382 67 000 001",
                "Podgorica",
                null,
                RoleNames.Admin);

            var advertiser = await EnsureUserAsync(
                userManager,
                "advertiser@example.com",
                demoPassword!,
                "Marko Marković",
                "+382 67 111 222",
                "Bulevar Svetog Petra Cetinjskog 10, Podgorica",
                "Dom Plus nekretnine",
                RoleNames.Advertiser);

            var regularUser = await EnsureUserAsync(
                userManager,
                "user@example.com",
                demoPassword!,
                "Ana Jovanović",
                "+382 67 333 444",
                "Nikšić",
                null,
                RoleNames.User);

            if (!await context.Properties.AnyAsync())
            {
                var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

                var apartment = new Property
                {
                    Title = "Dvosoban stan u centru Podgorice",
                    Description = "Uređen dvosoban stan sa dnevnim boravkom, kuhinjom, terasom i parking mjestom. Nalazi se u blizini škole, prodavnica i javnog prevoza.",
                    CategoryId = categories["Stan"],
                    Purpose = "Prodaja",
                    City = "Podgorica",
                    Address = "Ulica Slobode 25",
                    Area = 68,
                    Price = 145000,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UserId = advertiser.Id,
                    Images = new List<PropertyImage>
                    {
                        new() { ImagePath = "/images/property-placeholder.svg", IsPrimary = true }
                    }
                };

                var house = new Property
                {
                    Title = "Porodična kuća sa dvorištem",
                    Description = "Prostrana kuća na mirnoj lokaciji sa velikim dvorištem, garažom i pomoćnim objektom. Pogodna za porodični život.",
                    CategoryId = categories["Kuća"],
                    Purpose = "Izdavanje",
                    City = "Nikšić",
                    Address = "Novaka Ramova 14",
                    Area = 155,
                    Price = 850,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UserId = advertiser.Id,
                    Images = new List<PropertyImage>
                    {
                        new() { ImagePath = "/images/property-placeholder.svg", IsPrimary = true }
                    }
                };

                context.Properties.AddRange(apartment, house);
                await context.SaveChangesAsync();

                context.Reservations.Add(new Reservation
                {
                    PropertyId = apartment.Id,
                    UserId = regularUser.Id,
                    AppointmentDateTime = DateTime.Today.AddDays(3).AddHours(11),
                    Note = "Molim vas da potvrdite da li je termin dostupan.",
                    Status = ReservationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
        }

        private static bool IsStrongDemoPassword(string? password)
        {
            return password is { Length: >= 12 }
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(character => !char.IsLetterOrDigit(character));
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string fullName,
            string phoneNumber,
            string address,
            string? companyName,
            string role)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    PhoneNumberConfirmed = true,
                    Address = address,
                    CompanyName = companyName,
                    LockoutEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Nije moguće kreirati nalog {email}: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Nije moguće dodijeliti ulogu {role} nalogu {email}: {errors}");
                }
            }

            return user;
        }
    }
}
