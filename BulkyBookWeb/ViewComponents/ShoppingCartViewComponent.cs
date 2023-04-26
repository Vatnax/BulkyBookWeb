using BulkyBook.DataAccess.Repository;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BulkyBookWeb.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {
        private readonly IUnitOfWork _uow;

        public ShoppingCartViewComponent(IUnitOfWork uow)
        {
            _uow  = uow;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var claimValue = claim?.Value;

            if (claim != null)
            {
                if (HttpContext.Session.GetInt32(StaticNames.SessionCart) != null)
                {
                    return View(HttpContext.Session.GetInt32(StaticNames.SessionCart));
                }
                else
                {
                    HttpContext.Session.SetInt32(StaticNames.SessionCart,
                        _uow.ShoppingCartRepository.GetAll(u => u.ApplicationUserId == claimValue).ToList().Count);
                    return View(HttpContext.Session.GetInt32(StaticNames.SessionCart));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
