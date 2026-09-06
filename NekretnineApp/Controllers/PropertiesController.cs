using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NekretnineApp.Models;
using NekretnineApp.Models.ViewModels;
using NekretnineApp.Repositories;
using NekretnineApp.Services;

namespace NekretnineApp.Controllers
{
    public class PropertiesController : Controller
    {
        private const long MaximumPhotoSize = 5 * 1024 * 1024;
        private const int MaximumPhotoCount = 8;

        private readonly IPropertyRepository _propertyRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _environment;

        public PropertiesController(
            IPropertyRepository propertyRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment environment)
        {
            _propertyRepository = propertyRepository;
            _categoryRepository = categoryRepository;
            _environment = environment;
        }

        public async Task<IActionResult> Index([FromQuery] PropertyFilter filter)
        {
            var model = new PropertyIndexViewModel
            {
                Filter = filter,
                Properties = await _propertyRepository.GetFilteredAsync(filter, User.IsInRole(RoleNames.Admin)),
                Categories = await _categoryRepository.GetAllAsync(activeOnly: true),
                Cities = await _propertyRepository.GetCitiesAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!property.IsActive && !User.IsInRole(RoleNames.Admin) && property.UserId != currentUserId)
                return NotFound();

            return View(property);
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();
            return View(new PropertyFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Create(PropertyFormViewModel model)
        {
            await ValidatePropertyFormAsync(model, requirePhoto: true, existingPhotoCount: 0);

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var property = new Property
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                CategoryId = model.CategoryId,
                Purpose = model.Purpose,
                City = model.City.Trim(),
                Address = model.Address.Trim(),
                Area = model.Area,
                Price = model.Price,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };

            var photos = await SavePhotosAsync(model.Photos);
            for (var i = 0; i < photos.Count; i++)
            {
                photos[i].IsPrimary = i == 0;
                property.Images.Add(photos[i]);
            }

            await _propertyRepository.AddAsync(property);
            await _propertyRepository.SaveAsync();

            TempData["Success"] = "Oglas je uspješno dodat.";
            return RedirectToAction(nameof(MyProperties));
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> MyProperties()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var filter = new PropertyFilter();
            var properties = User.IsInRole(RoleNames.Admin)
                ? await _propertyRepository.GetFilteredAsync(filter, includeInactive: true)
                : await _propertyRepository.GetByUserIdAsync(userId);

            return View(properties);
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            if (!CanManage(property))
                return Forbid();

            var model = new PropertyFormViewModel
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                CategoryId = property.CategoryId,
                Purpose = property.Purpose,
                City = property.City,
                Address = property.Address,
                Area = property.Area,
                Price = property.Price,
                ExistingImages = property.Images.OrderByDescending(i => i.IsPrimary).ToList()
            };

            await LoadCategoriesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Edit(int id, PropertyFormViewModel model)
        {
            if (id != model.Id)
                return NotFound();

            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            if (!CanManage(property))
                return Forbid();

            var imagesToRemove = property.Images
                .Where(i => model.RemoveImageIds.Contains(i.Id))
                .ToList();

            var remainingPhotoCount = property.Images.Count - imagesToRemove.Count;
            await ValidatePropertyFormAsync(model, requirePhoto: false, existingPhotoCount: remainingPhotoCount);

            if (remainingPhotoCount + model.Photos.Count == 0)
                ModelState.AddModelError(nameof(model.Photos), "Oglas mora imati najmanje jednu fotografiju.");

            if (!ModelState.IsValid)
            {
                model.ExistingImages = property.Images.OrderByDescending(i => i.IsPrimary).ToList();
                await LoadCategoriesAsync();
                return View(model);
            }

            foreach (var image in imagesToRemove)
            {
                DeletePhysicalFile(image.ImagePath);
                _propertyRepository.DeleteImage(image);
            }

            var newImages = await SavePhotosAsync(model.Photos);
            foreach (var image in newImages)
                property.Images.Add(image);

            var remainingImages = property.Images
                .Where(i => !imagesToRemove.Contains(i))
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.Id)
                .ToList();

            foreach (var image in remainingImages)
                image.IsPrimary = false;
            if (remainingImages.Count > 0)
                remainingImages[0].IsPrimary = true;

            property.Title = model.Title.Trim();
            property.Description = model.Description.Trim();
            property.CategoryId = model.CategoryId;
            property.Purpose = model.Purpose;
            property.City = model.City.Trim();
            property.Address = model.Address.Trim();
            property.Area = model.Area;
            property.Price = model.Price;

            _propertyRepository.Update(property);
            await _propertyRepository.SaveAsync();

            TempData["Success"] = "Podaci o oglasu su uspješno izmijenjeni.";
            return RedirectToAction(nameof(MyProperties));
        }

        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            if (!CanManage(property))
                return Forbid();

            return View(property);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            if (!CanManage(property))
                return Forbid();

            foreach (var image in property.Images)
                DeletePhysicalFile(image.ImagePath);

            _propertyRepository.Delete(property);
            await _propertyRepository.SaveAsync();

            TempData["Success"] = "Oglas je obrisan.";
            return RedirectToAction(nameof(MyProperties));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Advertiser + "," + RoleNames.Admin)]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null)
                return NotFound();

            if (!CanManage(property))
                return Forbid();

            property.IsActive = !property.IsActive;
            _propertyRepository.Update(property);
            await _propertyRepository.SaveAsync();

            TempData["Success"] = property.IsActive ? "Oglas je aktiviran." : "Oglas je deaktiviran.";
            return RedirectToAction(nameof(MyProperties));
        }

        private async Task ValidatePropertyFormAsync(PropertyFormViewModel model, bool requirePhoto, int existingPhotoCount)
        {
            model.Photos = model.Photos.Where(photo => photo.Length > 0).ToList();

            var category = await _categoryRepository.GetByIdAsync(model.CategoryId);
            if (category == null || (!category.IsActive && !User.IsInRole(RoleNames.Admin)))
                ModelState.AddModelError(nameof(model.CategoryId), "Izabrana kategorija nije dostupna.");

            if (model.Purpose != "Prodaja" && model.Purpose != "Izdavanje")
                ModelState.AddModelError(nameof(model.Purpose), "Izaberite ispravnu namjenu.");

            if (requirePhoto && model.Photos.Count == 0)
                ModelState.AddModelError(nameof(model.Photos), "Dodajte najmanje jednu fotografiju.");

            if (existingPhotoCount + model.Photos.Count > MaximumPhotoCount)
                ModelState.AddModelError(nameof(model.Photos), $"Oglas može imati najviše {MaximumPhotoCount} fotografija.");

            foreach (var photo in model.Photos)
            {
                if (photo.Length > MaximumPhotoSize)
                    ModelState.AddModelError(nameof(model.Photos), "Svaka fotografija mora biti manja od 5 MB.");

                if (!await ImageFileValidator.IsValidAsync(photo, HttpContext.RequestAborted))
                    ModelState.AddModelError(nameof(model.Photos), "Datoteka mora biti ispravna JPG, PNG ili WEBP fotografija.");
            }
        }

        private async Task<List<PropertyImage>> SavePhotosAsync(IEnumerable<IFormFile> photos)
        {
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "properties");
            Directory.CreateDirectory(uploadFolder);

            var result = new List<PropertyImage>();
            foreach (var photo in photos)
            {
                var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var physicalPath = Path.Combine(uploadFolder, fileName);

                await using var stream = System.IO.File.Create(physicalPath);
                await photo.CopyToAsync(stream);

                result.Add(new PropertyImage
                {
                    ImagePath = $"/uploads/properties/{fileName}",
                    UploadedAt = DateTime.UtcNow
                });
            }

            return result;
        }

        private void DeletePhysicalFile(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || imagePath == "/images/property-placeholder.svg")
                return;

            var relativePath = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_environment.WebRootPath, relativePath);
            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);
        }

        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _categoryRepository.GetAllAsync(activeOnly: !User.IsInRole(RoleNames.Admin));
        }

        private bool CanManage(Property property)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return User.IsInRole(RoleNames.Admin) || property.UserId == currentUserId;
        }
    }
}
