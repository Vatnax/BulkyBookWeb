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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork db, IWebHostEnvironment webHostEnvironment)
        {
            _uow = db;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Product product = new();
            ProductVM productVM = new()
            {
                Product = new Product(),
                CategoryList = _uow.CategoryRepository.GetAll().Select(o => new SelectListItem
                {
                    Text = o.Name,
                    Value = o.Id.ToString()
                }),
                CoverTypeList = _uow.CoverTypeRepository.GetAll().Select(o => new SelectListItem
                {
                    Text = o.Name,
                    Value = o.Id.ToString()
                })
            };

            if (id == null || id == 0)
            {
                return View(productVM);
            }

            productVM.Product = _uow.ProductRepository.GetFirstOrDefault(u => u.Id == id);
            return View(productVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString();
                    var uploads = Path.Combine(wwwRootPath, @"images\products");
                    var extension = Path.GetExtension(file.FileName);

                    if (obj.Product.ImageUrl != null)
                    {
                        string oldImagePath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }
                    obj.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }

                if (obj.Product.Id == 0)
                    _uow.ProductRepository.Add(obj.Product);
                else
                    _uow.ProductRepository.Update(obj.Product);
                _uow.Save();
                TempData["success"] = "Product created successfully!";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Json(new { data = _uow.ProductRepository.GetAll(includeProperties: "Category,CoverType") });
        }

        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _uow.ProductRepository.GetFirstOrDefault(u => u.Id == id);

            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting!" });
            }

            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _uow.ProductRepository.Remove(obj);
            _uow.Save();
            return Json(new { success = true, message = "Delete successful!" });
        }
    }
}
