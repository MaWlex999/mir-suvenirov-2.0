using Microsoft.AspNetCore.Mvc;
using MirSuvenirov.Data;
using MirSuvenirov.Models;
using System.Net.Http.Headers;

namespace MirSuvenirov.Controllers
{
    public class CatalogController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };
        private const long MaxImageFileSize = 5 * 1024 * 1024;

        public CatalogController(AppDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        public IActionResult Index(
            string search,
            string category,
            string material,
            string availability,
            int? priceMin,
            int? priceMax,
            string sort)
        {
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
                productsQuery = productsQuery.Where(p => p.InStock == true);
            else if (availability == "onOrder")
                productsQuery = productsQuery.Where(p => p.InStock == false);

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

            var viewModel = new ProductFilterViewModel
            {
                Products = productsQuery.ToList(),
                Query = search,
                Category = category,
                Material = material,
                Availability = availability,
                Sort = sort,
                PriceMin = priceMin,
                PriceMax = priceMax,
                Categories = _db.Products.Select(p => p.Category).Distinct().ToList(),
                Materials = _db.Products.Select(p => p.Material).Distinct().ToList()
            };

            return View(viewModel);
        }

        [HttpGet("/boxes")]
        public IActionResult Boxes()
        {
            return Category("box", "Шкатулки ручной работы", "Уникальные шкатулки из натуральных материалов для хранения драгоценностей и памятных вещей", "");
        }

        [HttpGet("/lamps")]
        public IActionResult Lamps()
        {
            return Category("lamp", "Светильники и лампы", "Создайте уютную атмосферу с нашими уникальными светильниками ручной работы", "lamps-hero");
        }

        [HttpGet("/pens")]
        public IActionResult Pens()
        {
            return Category("pen", "Письменные принадлежности", "Элегантные ручки и аксессуары для письма премиум-класса", "pens-hero");
        }

        [HttpGet("/holders")]
        public IActionResult Holders()
        {
            return Category("holder", "Держатели и органайзеры", "Практичные и стильные держатели для организации пространства", "holders-hero");
        }

        [HttpGet("/add-product")]
        public IActionResult AddProduct()
        {
            return View(new AddProductViewModel());
        }

        [HttpPost("/add-product")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(AddProductViewModel model)
        {
            if ((model.ImageFile == null || model.ImageFile.Length == 0) && string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                ModelState.AddModelError(nameof(model.ImageFile), "Загрузите файл изображения или укажите ссылку/путь.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var imagePath = NormalizeImagePath(model.ImageUrl);
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadedPath = await SaveProductImageAsync(model.ImageFile);
                if (uploadedPath == null)
                {
                    return View(model);
                }

                imagePath = uploadedPath;
            }
            else if (IsAbsoluteHttpUrl(imagePath))
            {
                var downloadedPath = await DownloadAndSaveRemoteImageAsync(imagePath!);
                if (downloadedPath == null)
                {
                    ModelState.AddModelError(nameof(model.ImageUrl), "Не удалось загрузить изображение по ссылке. Загрузите файл или укажите прямую ссылку на изображение.");
                    return View(model);
                }

                imagePath = downloadedPath;
            }

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                ModelState.AddModelError(nameof(model.ImageUrl), "Не удалось определить путь изображения.");
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Price = model.Price,
                Category = model.Category,
                Description = model.Description,
                Image = imagePath,
                Material = model.Material,
                InStock = model.InStock
            };

            _db.Products.Add(product);
            _db.SaveChanges();

            TempData["ToastMessage"] = $"Товар \"{product.Name}\" добавлен.";
            TempData["ToastType"] = "success";
            return RedirectToAction(nameof(Index));
        }

        private string? NormalizeImagePath(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var normalized = imageUrl.Trim().Replace('\\', '/');

            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return normalized;
            }

            return normalized.TrimStart('/');
        }

        private static bool IsAbsoluteHttpUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private async Task<string?> DownloadAndSaveRemoteImageAsync(string imageUrl)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            using var response = await httpClient.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/"))
            {
                return null;
            }

            var extension = ResolveImageExtension(response.Content.Headers.ContentType, imageUrl);
            if (extension == null)
            {
                return null;
            }

            var uploadDirectory = GetProductUploadsDirectory(out var relativePrefix);
            var safeFileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(uploadDirectory, safeFileName);

            await using (var destination = new FileStream(absolutePath, FileMode.Create))
            await using (var source = await response.Content.ReadAsStreamAsync())
            {
                await source.CopyToAsync(destination);
            }

            var info = new FileInfo(absolutePath);
            if (info.Length <= 0 || info.Length > MaxImageFileSize)
            {
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                }
                return null;
            }

            return $"{relativePrefix}/{safeFileName}";
        }

        private static string? ResolveImageExtension(MediaTypeHeaderValue? contentType, string sourceUrl)
        {
            var mediaType = contentType?.MediaType?.ToLowerInvariant() ?? string.Empty;
            var extension = mediaType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => null
            };

            if (extension != null && AllowedImageExtensions.Contains(extension))
            {
                return extension;
            }

            var urlExtension = Path.GetExtension(new Uri(sourceUrl).AbsolutePath).ToLowerInvariant();
            return AllowedImageExtensions.Contains(urlExtension) ? urlExtension : null;
        }

        private string GetProductUploadsDirectory(out string relativePrefix)
        {
            var now = DateTime.UtcNow;
            var relativeSegments = new[] { "uploads", "products", now.ToString("yyyy"), now.ToString("MM") };
            relativePrefix = string.Join("/", relativeSegments);

            var webRootPath = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;

            var uploadDirectory = Path.Combine(webRootPath, Path.Combine(relativeSegments));
            Directory.CreateDirectory(uploadDirectory);
            return uploadDirectory;
        }

        private async Task<string?> SaveProductImageAsync(IFormFile file)
        {
            if (file.Length <= 0)
            {
                ModelState.AddModelError(nameof(AddProductViewModel.ImageFile), "Файл изображения пустой.");
                return null;
            }

            if (file.Length > MaxImageFileSize)
            {
                ModelState.AddModelError(nameof(AddProductViewModel.ImageFile), "Размер файла не должен превышать 5 МБ.");
                return null;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(AddProductViewModel.ImageFile), "Допустимые форматы: .jpg, .jpeg, .png, .webp, .gif.");
                return null;
            }

            var uploadDirectory = GetProductUploadsDirectory(out var relativePrefix);

            var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var absolutePath = Path.Combine(uploadDirectory, safeFileName);

            await using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"{relativePrefix}/{safeFileName}";
        }

        private IActionResult Category(string code, string title, string description, string heroClass)
        {
            var model = new CategoryPageViewModel
            {
                Title = title,
                Description = description,
                HeroClass = heroClass,
                Products = _db.Products.Where(p => p.Category == code).OrderBy(p => p.Id).ToList()
            };

            return View("Category", model);
        }
    }
}