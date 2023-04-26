using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticNames.RoleUserAdmin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _uow;

        public CategoryController(IUnitOfWork db)
        {
            _uow = db;
        }

        public IActionResult Index()
        {
            return View(_uow.CategoryRepository.GetAll());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
                ModelState.AddModelError("Name", "Name cannot be the same as display order!");

            if (ModelState.IsValid)
            {
                _uow.CategoryRepository.Add(obj);
                _uow.Save();
                TempData["success"] = "Created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var category = _uow.CategoryRepository.GetFirstOrDefault(u => u.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
                ModelState.AddModelError("Name", "Name cannot be the same as display order!");

            if (ModelState.IsValid)
            {
                _uow.CategoryRepository.Update(obj);
                _uow.Save();
                TempData["success"] = "Edited successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            var category = _uow.CategoryRepository.GetFirstOrDefault(u => u.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Category obj)
        {
            _uow.CategoryRepository.Remove(obj);
            _uow.Save();
            TempData["success"] = "Deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
