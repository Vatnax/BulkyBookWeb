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
    public class CoverTypeController : Controller
    {
        private readonly IUnitOfWork _uow;

        public CoverTypeController(IUnitOfWork db)
        {
            _uow = db;
        }

        public IActionResult Index()
        {
            return View(_uow.CoverTypeRepository.GetAll());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _uow.CoverTypeRepository.Add(obj);
                _uow.Save();
                TempData["success"] = "Created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(obj);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null) return NotFound();
            var coverType = _uow.CoverTypeRepository.GetFirstOrDefault(u => u.Id == id);
            if (coverType == null) return NotFound();

            return View(coverType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CoverType obj)
        {
            if (ModelState.IsValid)
            {
                _uow.CoverTypeRepository.Update(obj);
                _uow.Save();
                TempData["success"] = "Edited successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(obj);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            var coverType = _uow.CoverTypeRepository.GetFirstOrDefault(u => u.Id == id);
            if (coverType == null) return NotFound();

            return View(coverType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(CoverType obj)
        {
            _uow.CoverTypeRepository.Remove(obj);
            _uow.Save();
            TempData["success"] = "Deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
