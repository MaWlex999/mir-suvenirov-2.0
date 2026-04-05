using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MirSuvenirov.Data;
using MirSuvenirov.Models;

namespace MirSuvenirov.Controllers
{
    public class FavoritesController : Controller
    {
        private readonly AppDbContext _db;

        public FavoritesController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var items = _db.Favorites
                .Include(x => x.Product)
                .OrderByDescending(x => x.Id)
                .ToList();

            return View(items);
        }

        [HttpPost]
        public IActionResult Toggle(int productId, string returnUrl)
        {
            var product = _db.Products.Find(productId);
            if (product == null)
            {
                return RedirectToAction("Index", "Catalog");
            }

            var existing = _db.Favorites.FirstOrDefault(x => x.ProductId == productId);
            if (existing == null)
            {
                _db.Favorites.Add(new FavoriteItem
                {
                    ProductId = productId
                });
                TempData["ToastMessage"] = $"Товар \"{product.Name}\" добавлен в избранное.";
                TempData["ToastType"] = "success";
            }
            else
            {
                _db.Favorites.Remove(existing);
                TempData["ToastMessage"] = $"Товар \"{product.Name}\" удален из избранного.";
                TempData["ToastType"] = "success";
            }

            _db.SaveChanges();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int id)
        {
            var item = _db.Favorites.Include(x => x.Product).FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _db.Favorites.Remove(item);
                _db.SaveChanges();
                TempData["ToastMessage"] = $"Товар \"{item.Product.Name}\" удален из избранного.";
                TempData["ToastType"] = "success";
            }

            return RedirectToAction("Index");
        }
    }
}
