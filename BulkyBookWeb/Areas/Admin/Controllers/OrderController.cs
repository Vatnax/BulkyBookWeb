using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.ViewModels;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _uow;
        [BindProperty]
        public OrderVM OrderVM { get; set; }

        public OrderController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetails = _uow.OrderDetailRepository.GetAll(o => o.OrderId == orderId, includeProperties: "Product")
            };

            return View(OrderVM);
        }

        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetails = _uow.OrderDetailRepository.GetAll(o => o.OrderId == OrderVM.OrderHeader.Id, includeProperties: "Product");


            var domain = "https://localhost:44350/";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderid={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/Details?orderId={OrderVM.OrderHeader.Id}",
            };

            foreach (var item in OrderVM.OrderDetails)
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
            OrderVM.OrderHeader.PaymentDate = DateTime.Now;
            OrderVM.OrderHeader.SessionId = session.Id;
            OrderVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
            _uow.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            OrderHeader orderHeader = _uow.OrderHeaderRepository.GetFirstOrDefault(u => u.Id == orderHeaderid);
            if (orderHeader.PaymentStatus == StaticNames.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                //check the stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    orderHeader.OrderStatus = orderHeader.OrderStatus;
                    orderHeader.PaymentStatus = StaticNames.PaymentStatusApproved;
                    _uow.OrderHeaderRepository.Update(orderHeader);
                    _uow.Save();
                }
            }
            return View(orderHeaderid);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticNames.RoleUserAdmin + "," + StaticNames.RoleUserEmployee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderHeaderFromDb = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == OrderVM.OrderHeader.Id, tracked: false);

            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (OrderVM.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (OrderVM.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _uow.OrderHeaderRepository.Update(orderHeaderFromDb);
            _uow.Save();
            TempData["Success"] = "Order details updated successfully";
            return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticNames.RoleUserAdmin + "," + StaticNames.RoleUserEmployee)]
        public IActionResult StartProcessing()
        {
            var orderHeaderFromDb = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == OrderVM.OrderHeader.Id, tracked: false);
            orderHeaderFromDb.OrderStatus = StaticNames.StatusInProcess;
            _uow.OrderHeaderRepository.Update(orderHeaderFromDb);
            _uow.Save();
            TempData["Success"] = "Order Status updated successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticNames.RoleUserAdmin + "," + StaticNames.RoleUserEmployee)]
        public IActionResult ShipOrder()
        {
            var orderHeaderFromDb = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == OrderVM.OrderHeader.Id, tracked: false);
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = StaticNames.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;
            _uow.OrderHeaderRepository.Update(orderHeaderFromDb);
            _uow.Save();
            TempData["Success"] = "Order Shipped updated successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticNames.RoleUserAdmin + "," + StaticNames.RoleUserEmployee)]
        public IActionResult CancelOrder()
        {
            var orderHeaderFromDb = _uow.OrderHeaderRepository.GetFirstOrDefault(o => o.Id == OrderVM.OrderHeader.Id, tracked: true);

            if (orderHeaderFromDb.PaymentStatus == StaticNames.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId,
                };

                var service = new RefundService();
                Refund refund = service.Create(options);
                orderHeaderFromDb.OrderStatus = StaticNames.StatusCancelled;
                orderHeaderFromDb.PaymentStatus = StaticNames.StatusRefunded;
            }
            else
            {
                orderHeaderFromDb.OrderStatus = StaticNames.StatusCancelled;
                orderHeaderFromDb.PaymentStatus = StaticNames.StatusCancelled;
            }

            _uow.OrderHeaderRepository.Update(orderHeaderFromDb);
            _uow.Save();
            TempData["Success"] = "Order Shipped updated successfully";
            return RedirectToAction("Details", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }

        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;
            orderHeaders = _uow.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser");

            if (User.IsInRole(StaticNames.RoleUserAdmin) || User.IsInRole(StaticNames.RoleUserEmployee))
            {
                orderHeaders = _uow.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _uow.OrderHeaderRepository.GetAll(u => u.ApplicationUserId == claim.Value, includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u => u.PaymentStatus == StaticNames.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticNames.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticNames.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == StaticNames.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new { data = orderHeaders });
        }
    }
}
