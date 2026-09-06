using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;

namespace NekretnineApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var model = Map(user, roles.FirstOrDefault() ?? string.Empty);
            return View(model);
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            return View(Map(user, roles.FirstOrDefault() ?? string.Empty));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            model.Email = user.Email ?? string.Empty;
            model.Role = roles.FirstOrDefault() ?? string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            user.FullName = model.FullName.Trim();
            user.CompanyName = string.IsNullOrWhiteSpace(model.CompanyName) ? null : model.CompanyName.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();
            user.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            TempData["Success"] = "Profil je uspješno izmijenjen.";
            return RedirectToAction(nameof(Index));
        }

        private static ProfileViewModel Map(ApplicationUser user, string role)
        {
            return new ProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                CompanyName = user.CompanyName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = role
            };
        }
    }
}
