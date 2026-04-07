using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MirSuvenirov.Data;
using MirSuvenirov.Models;

namespace MirSuvenirov.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public ApiController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        [HttpGet("products")]
        public IActionResult GetProducts()
        {
            var products = _db.Products.ToList();
            return Ok(products);
        }

        [HttpGet("search")]
        public IActionResult Search(string? q)
        {
            if (string.IsNullOrEmpty(q))
                return Ok(new { ok = true, products = _db.Products.ToList() });

            var lower = q.ToLower();
            var result = _db.Products.Where(p =>
                p.Name.ToLower().Contains(lower) ||
                p.Description.ToLower().Contains(lower) ||
                p.Category.ToLower().Contains(lower) ||
                p.Material.ToLower().Contains(lower)
            ).ToList();

            return Ok(new { ok = true, products = result });
        }

        [HttpGet("catalog")]
        public IActionResult Catalog(
            string? search,
            string? category,
            string? material,
            string? availability,
            int? priceMin,
            int? priceMax,
            string? sort,
            int page = 1,
            int pageSize = 4)
        {
            pageSize = 4;
            if (page < 1)
            {
                page = 1;
            }

            var productsQuery = _db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lower = search.ToLower();
                productsQuery = productsQuery.Where(p =>
                    p.Name.ToLower().Contains(lower) ||
                    p.Description.ToLower().Contains(lower) ||
                    p.Category.ToLower().Contains(lower) ||
                    p.Material.ToLower().Contains(lower));
            }

            if (!string.IsNullOrEmpty(category) && category != "all")
                productsQuery = productsQuery.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(material) && material != "all")
                productsQuery = productsQuery.Where(p => p.Material == material);

            if (availability == "inStock")
                productsQuery = productsQuery.Where(p => p.InStock);
            else if (availability == "onOrder")
                productsQuery = productsQuery.Where(p => !p.InStock);

            if (priceMin.HasValue)
                productsQuery = productsQuery.Where(p => p.Price >= priceMin.Value);

            if (priceMax.HasValue)
                productsQuery = productsQuery.Where(p => p.Price <= priceMax.Value);

            productsQuery = sort switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "name_asc" => productsQuery.OrderBy(p => p.Name),
                "name_desc" => productsQuery.OrderByDescending(p => p.Name),
                _ => productsQuery.OrderBy(p => p.Id)
            };

            var totalCount = productsQuery.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages)
            {
                page = totalPages;
            }

            var products = productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                ok = true,
                products = products,
                total = totalCount,
                totalPages = totalPages,
                page = page,
                pageSize = pageSize
            });
        }

        [HttpGet("products/{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null)
                return Ok(new { ok = false, error = "Товар не найден" });

            return Ok(new { ok = true, product = product });
        }

        [HttpDelete("products/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = _db.Products.Find(id);
            if (product == null)
            {
                return Ok(new { ok = false, error = "Товар не найден" });
            }

            var basketItems = _db.Baskets.Where(x => x.ProductId == id).ToList();
            if (basketItems.Count > 0)
            {
                _db.Baskets.RemoveRange(basketItems);
            }

            var favoriteItems = _db.Favorites.Where(x => x.ProductId == id).ToList();
            if (favoriteItems.Count > 0)
            {
                _db.Favorites.RemoveRange(favoriteItems);
            }

            _db.Products.Remove(product);
            _db.SaveChanges();

            TryDeleteLocalImage(product.Image);
            return Ok(new { ok = true, deletedId = id, productName = product.Name });
        }

        [HttpGet("basket")]
        public IActionResult GetBasket()
        {
            var items = _db.Baskets.Include(b => b.Product).ToList();
            return Ok(new { ok = true, items = items, totalItems = items.Sum(i => i.Quantity) });
        }

        [HttpPost("basket")]
        public IActionResult AddToBasket([FromBody] BasketRequest request)
        {
            var product = _db.Products.Find(request.ProductId);
            if (product == null)
                return Ok(new { ok = false, error = "Товар не найден" });

            var existing = _db.Baskets.FirstOrDefault(b => b.ProductId == request.ProductId);
            if (existing != null)
                existing.Quantity++;
            else
                _db.Baskets.Add(new BasketItem { ProductId = request.ProductId, Quantity = 1 });

            _db.SaveChanges();

            var totalItems = _db.Baskets.Sum(b => b.Quantity);
            return Ok(new { ok = true, totalItems = totalItems, productName = product.Name });
        }

        [HttpDelete("basket/{id}")]
        public IActionResult RemoveFromBasket(int id)
        {
            var item = _db.Baskets.Find(id);
            if (item == null)
                return Ok(new { ok = false });

            _db.Baskets.Remove(item);
            _db.SaveChanges();

            var totalItems = _db.Baskets.Sum(b => b.Quantity);
            return Ok(new { ok = true, totalItems = totalItems });
        }

        [HttpGet("favorites")]
        public IActionResult GetFavorites()
        {
            var items = _db.Favorites.Include(f => f.Product).ToList();
            return Ok(new { ok = true, items = items });
        }

        [HttpPost("favorites")]
        public IActionResult ToggleFavorite([FromBody] FavoriteRequest request)
        {
            var product = _db.Products.Find(request.ProductId);
            if (product == null)
                return Ok(new { ok = false, error = "Товар не найден" });

            var existing = _db.Favorites.FirstOrDefault(f => f.ProductId == request.ProductId);
            bool added;
            if (existing != null)
            {
                _db.Favorites.Remove(existing);
                added = false;
            }
            else
            {
                _db.Favorites.Add(new FavoriteItem { ProductId = request.ProductId });
                added = true;
            }

            _db.SaveChanges();
            return Ok(new { ok = true, added = added, productName = product.Name });
        }

        [HttpDelete("favorites/{id}")]
        public IActionResult RemoveFromFavorites(int id)
        {
            var item = _db.Favorites.Find(id);
            if (item == null)
                return Ok(new { ok = false });

            _db.Favorites.Remove(item);
            _db.SaveChanges();
            return Ok(new { ok = true });
        }

        private void TryDeleteLocalImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return;
            }

            var normalized = imagePath.Replace('\\', '/').Trim().TrimStart('/');
            if (!normalized.StartsWith("uploads/products/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var absolutePath = Path.GetFullPath(Path.Combine(webRootPath, normalized));
            var webRootFullPath = Path.GetFullPath(webRootPath);

            if (!absolutePath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(absolutePath))
            {
                System.IO.File.Delete(absolutePath);
            }
        }
    }

    public class BasketRequest
    {
        public int ProductId { get; set; }
    }

    public class FavoriteRequest
    {
        public int ProductId { get; set; }
    }
}
