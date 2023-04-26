using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.ViewModels;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _uow;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork uow)
        {
            _logger = logger;
            _uow = uow;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> products = _uow.ProductRepository.GetAll(includeProperties: "Category,CoverType");
            return View(products);
        }

        public IActionResult Details(int productId)
        {
            Product product = _uow.ProductRepository.GetFirstOrDefault(u => u.Id == productId, includeProperties: "Category,CoverType");
            ShoppingCart shoppingCart = new ShoppingCart()
            {
                Product = product,
                ProductId = productId,
                Count = 1
            };
            return View(shoppingCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.ApplicationUserId = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ShoppingCart shoppingFromDb = _uow.ShoppingCartRepository.GetFirstOrDefault(x =>
                shoppingCart.ApplicationUserId == x.ApplicationUserId && x.ProductId == shoppingCart.ProductId);

            if (shoppingFromDb == null)
            {
                _uow.ShoppingCartRepository.Add(shoppingCart);
                _uow.Save();
                HttpContext.Session.SetInt32(StaticNames.SessionCart, _uow.ShoppingCartRepository.GetAll(
                    u => u.ApplicationUserId == (User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.NameIdentifier).Value).ToList().Count);
            }
            else
            {
                _uow.ShoppingCartRepository.IncrementCount(shoppingFromDb, shoppingCart.Count);
                _uow.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}