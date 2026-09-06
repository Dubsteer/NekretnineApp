using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NekretnineApp.Models;
using NekretnineApp.Repositories;

namespace NekretnineApp.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class CategoriesController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoriesController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _categoryRepository.GetAllAsync());
        }

        public IActionResult Create()
        {
            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            category.Name = category.Name?.Trim() ?? string.Empty;
            if (await _categoryRepository.NameExistsAsync(category.Name))
                ModelState.AddModelError(nameof(category.Name), "Kategorija sa ovim nazivom već postoji.");

            if (!ModelState.IsValid)
                return View(category);

            category.IsActive = true;
            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = "Kategorija je uspješno dodata.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return category == null ? NotFound() : View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id)
                return NotFound();

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            model.Name = model.Name?.Trim() ?? string.Empty;
            if (await _categoryRepository.NameExistsAsync(model.Name, id))
                ModelState.AddModelError(nameof(model.Name), "Kategorija sa ovim nazivom već postoji.");

            if (!ModelState.IsValid)
                return View(model);

            category.Name = model.Name;
            category.IsActive = model.IsActive;
            _categoryRepository.Update(category);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = "Kategorija je uspješno izmijenjena.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            category.IsActive = !category.IsActive;
            _categoryRepository.Update(category);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = category.IsActive ? "Kategorija je aktivirana." : "Kategorija je deaktivirana.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            if (await _categoryRepository.HasPropertiesAsync(id))
            {
                TempData["Error"] = "Kategoriju koja se koristi u oglasima nije moguće obrisati. Možete je deaktivirati.";
                return RedirectToAction(nameof(Index));
            }

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = "Kategorija je obrisana.";
            return RedirectToAction(nameof(Index));
        }
    }
}
