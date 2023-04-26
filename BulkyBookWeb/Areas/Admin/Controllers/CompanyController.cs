using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.ViewModels;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = StaticNames.RoleUserAdmin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _uow;

        public CompanyController(IUnitOfWork db)
        {
            _uow = db;
        }

        public IActionResult Index()
        {

            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Company company = new();
            if (id == 0 || id == null)
                return View(company);

            company = _uow.CompanyRepository.GetFirstOrDefault(o => o.Id == id);
            return View(company);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj)
        {
            if (ModelState.IsValid)
            {
                if (obj.Id == 0)
                {
                    _uow.CompanyRepository.Add(obj);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                    _uow.CompanyRepository.Update(obj);
                    TempData["success"] = "Company updated successfully!";

                }

                _uow.Save();
                return RedirectToAction("Index");
            }
            return View(obj);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Json(new { data = _uow.CompanyRepository.GetAll() });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _uow.CompanyRepository.GetFirstOrDefault(u => u.Id == id);

            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting!" });
            }
            _uow.CompanyRepository.Remove(obj);
            _uow.Save();
            return Json(new { success = true, message = "Delete successful!" });
        }
    }
}
