using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;
using NekretnineApp.Repositories;

namespace NekretnineApp.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly ICategoryRepository _categoryRepository;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IPropertyRepository propertyRepository,
            IReservationRepository reservationRepository,
            ICategoryRepository categoryRepository)
        {
            _userManager = userManager;
            _propertyRepository = propertyRepository;
            _reservationRepository = reservationRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            var recent = await _reservationRepository.GetRecentAsync(5);
            var activeProperties = await _propertyRepository.CountActiveAsync();
            var totalProperties = await _propertyRepository.CountAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalAdvertisers = (await _userManager.GetUsersInRoleAsync(RoleNames.Advertiser)).Count,
                TotalRegularUsers = (await _userManager.GetUsersInRoleAsync(RoleNames.User)).Count,
                TotalCategories = await _categoryRepository.CountAsync(),
                TotalProperties = totalProperties,
                ActiveProperties = activeProperties,
                InactiveProperties = totalProperties - activeProperties,
                TotalReservations = await _reservationRepository.CountAsync(),
                PendingReservations = await _reservationRepository.CountByStatusAsync(ReservationStatus.Pending),
                ApprovedReservations = await _reservationRepository.CountByStatusAsync(ReservationStatus.Approved),
                RecentReservations = recent.Select(r => new ReservationItemViewModel
                {
                    Reservation = r,
                    UserEmail = r.User?.Email ?? "Nepoznat korisnik",
                    UserName = r.User?.FullName ?? "Nepoznat korisnik",
                    UserPhone = r.User?.PhoneNumber ?? "Nije unesen"
                }).ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var model = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? "Nije unesen",
                    Role = roles.FirstOrDefault() ?? "Bez uloge",
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            if (!RoleNames.PublicRoles.Contains(role))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            {
                TempData["Error"] = "Administratorska uloga se ne može promijeniti na ovoj stranici.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(role))
            {
                TempData["Success"] = "Korisnik već ima izabranu ulogu.";
                return RedirectToAction(nameof(Users));
            }

            if (role == RoleNames.User &&
                currentRoles.Contains(RoleNames.Advertiser) &&
                await _propertyRepository.AnyByUserIdAsync(user.Id))
            {
                TempData["Error"] = "Ulogu oglašivača nije moguće promijeniti dok korisnik ima svoje oglase.";
                return RedirectToAction(nameof(Users));
            }

            var removableRoles = currentRoles.Where(r => RoleNames.PublicRoles.Contains(r)).ToList();
            if (removableRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, removableRoles);
                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = "Trenutnu ulogu nije moguće ukloniti.";
                    return RedirectToAction(nameof(Users));
                }
            }

            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                if (removableRoles.Count > 0)
                    await _userManager.AddToRolesAsync(user, removableRoles);

                TempData["Error"] = "Novu ulogu nije moguće dodijeliti.";
                return RedirectToAction(nameof(Users));
            }

            TempData["Success"] = "Uloga korisnika je uspješno promijenjena.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            {
                TempData["Error"] = "Administratorski nalog nije moguće zaključati.";
                return RedirectToAction(nameof(Users));
            }

            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, isLocked ? null : DateTimeOffset.UtcNow.AddYears(100));

            TempData["Success"] = isLocked ? "Korisnički nalog je otključan." : "Korisnički nalog je zaključan.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Reports()
        {
            var reservations = await _reservationRepository.GetAllAsync();
            var approved = reservations.Where(r => r.Status == ReservationStatus.Approved).ToList();
            var culture = CultureInfo.GetCultureInfo("sr-Latn-ME");

            var model = new ReportsViewModel
            {
                ByCity = approved
                    .Where(r => r.Property != null)
                    .GroupBy(r => r.Property!.City)
                    .Select(g => new CityReservationReportItem { City = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                ByProperty = approved
                    .Where(r => r.Property != null)
                    .GroupBy(r => r.Property!.Title)
                    .Select(g => new PropertyReservationReportItem { PropertyTitle = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList(),

                ByMonth = reservations
                    .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new MonthlyReservationReportItem
                    {
                        Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", culture),
                        Count = g.Count()
                    })
                    .ToList()
            };

            return View(model);
        }
    }
}
