using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MirSuvenirov.Data;
using MirSuvenirov.Models;

namespace MirSuvenirov.Controllers
{
    public class BasketController : Controller
    {
        private readonly AppDbContext _db;

        public BasketController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var items = _db.Baskets
                .Include(x => x.Product)
                .OrderByDescending(x => x.Id)
                .ToList();

            return View(items);
        }

        [HttpPost]
        public IActionResult Add(int productId, string returnUrl)
        {
            var product = _db.Products.Find(productId);
            if (product == null)
            {
                return RedirectToAction("Index");
            }

            var existing = _db.Baskets.FirstOrDefault(x => x.ProductId == productId);
            if (existing == null)
            {
                _db.Baskets.Add(new BasketItem
                {
                    ProductId = productId,
                    Quantity = 1
                });
            }
            else
            {
                existing.Quantity++;
            }

            _db.SaveChanges();
            TempData["ToastMessage"] = $"Товар \"{product.Name}\" добавлен в корзину.";
            TempData["ToastType"] = "success";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var item = _db.Baskets.Include(x => x.Product).FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _db.Baskets.Remove(item);
                _db.SaveChanges();
                TempData["ToastMessage"] = $"Товар \"{item.Product.Name}\" удален из корзины.";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("Index");
        }
    }
}
