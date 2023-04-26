using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.ViewModels;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public int OrderTotal { get; set; }

        public ShoppingCartController(IUnitOfWork uow, IEmailSender emailSender)
        {
            _uow = uow;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var claim = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _uow.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties: "Product"),
                OrderHeader = new()
            };

            foreach (var val in ShoppingCartVM.ListCart)
            {
                val.Price = GetPrice(val.Count, val.Product.Price, val.Product.Price50, val.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (val.Price * val.Count);
            }

            return View(ShoppingCartVM);
        }

        public double GetPrice(int quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
                return price;

            if (quantity <= 100)
                return price50;

            return price100;
        }

        public IActionResult Plus(int id)
        {
            var cart = _uow.ShoppingCartRepository.GetFirstOrDefault(u => u.Id == id, includeProperties: "ApplicationUser");
            _uow.ShoppingCartRepository.IncrementCount(cart, 1);
            _uow.Save();
            _emailSender.SendEmailAsync(cart.ApplicationUser.Email, "Minus", "WOW, YOU FUCKING CLICKED PLUS, GZ!!!");
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int id)
        {
            var cart = _uow.ShoppingCartRepository.GetFirstOrDefault(u => u.Id == id);
            int x = _uow.ShoppingCartRepository.DecrementCount(cart, 1);
            if (x <= 0)
            {
                _uow.ShoppingCartRepository.Remove(cart);
                HttpContext.Session.SetInt32(StaticNames.SessionCart, (int)HttpContext.Session.GetInt32(StaticNames.SessionCart) - 1);
            }
            _uow.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int id)
        {
            var cart = _uow.ShoppingCartRepository.GetFirstOrDefault(u => u.Id == id);
            _uow.ShoppingCartRepository.Remove(cart);
            _uow.Save();
            HttpContext.Session.SetInt32(StaticNames.SessionCart, (int)HttpContext.Session.GetInt32(StaticNames.SessionCart) - 1);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            var claim = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _uow.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _uow.ApplicationUserRepository.GetFirstOrDefault(
                u => u.Id == claim.Value);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var val in ShoppingCartVM.ListCart)
            {
                val.Price = GetPrice(val.Count, val.Product.Price, val.Product.Price50, val.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (val.Price * val.Count);
            }

            return View(ShoppingCartVM);
        }

        [ActionName("Summary")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            var claim = (User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM.ListCart = _uow.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claim.Value,
                    includeProperties: "Product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var val in ShoppingCartVM.ListCart)
            {
                val.Price = GetPrice(val.Count, val.Product.Price, val.Product.Price50, val.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (val.Price * val.Count);
            }

            ApplicationUser appUser = _uow.ApplicationUserRepository.GetFirstOrDefault(o => o.Id == claim.Value);
            if(appUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCartVM.OrderHeader.OrderStatus = StaticNames.StatusPending;
                ShoppingCartVM.OrderHeader.PaymentStatus = StaticNames.PaymentStatusPending;
            }
            else
            {
                ShoppingCartVM.OrderHeader.OrderStatus = StaticNames.StatusApproved;
                ShoppingCartVM.OrderHeader.PaymentStatus = StaticNames.PaymentStatusDelayedPayment;
            }
            _uow.OrderHeaderRepository.Add(ShoppingCartVM.OrderHeader);
            _uow.Save();

            foreach (var val in ShoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = val.ProductId,
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = val.Price,
                    Count = val.Count
                };
                _uow.OrderDetailRepository.Add(orderDetail);
                _uow.Save();
            }

            if (appUser.CompanyId.GetValueOrDefault() == 0)
            {
                var domain = "https://localhost:44350/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"Customer/ShoppingCart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"Customer/ShoppingCart/index",
                };

                foreach (var item in ShoppingCartVM.ListCart)
                {

                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),//20.00 -> 2000
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            },

                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);

                }

                var service = new SessionService();
                Session session = service.Create(options);
                ShoppingCartVM.OrderHeader.PaymentDate = DateTime.Now;
                ShoppingCartVM.OrderHeader.SessionId = session.Id;
                ShoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
                _uow.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            return RedirectToAction("OrderConfirmation", "ShoppingCart", new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == id);

            if(orderHeader.PaymentStatus != StaticNames.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _uow.OrderHeaderRepository.UpdateStatus(id, StaticNames.StatusApproved, StaticNames.PaymentStatusApproved);
                    _uow.Save();
                }
            }

            List<ShoppingCart> shoppingCarts = _uow.ShoppingCartRepository
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            HttpContext.Session.Clear();
            _uow.ShoppingCartRepository.RemoveRange(shoppingCarts);
            _uow.Save();

            return View(id);
        }
    }
}
