using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MirSuvenirov.Models
{
    public class AddProductViewModel
    {
        [Display(Name = "Название товара")]
        [Required(ErrorMessage = "Укажите название товара.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Название должно быть от 3 до 200 символов.")]
        public string Name { get; set; } = "";

        [Display(Name = "Цена")]
        [Required(ErrorMessage = "Укажите цену товара.")]
        [Range(100, 100000, ErrorMessage = "Цена должна быть в диапазоне от 100 до 100000 руб.")]
        public int Price { get; set; }

        [Display(Name = "Категория")]
        [Required(ErrorMessage = "Выберите категорию.")]
        [RegularExpression("box|lamp|pen|holder", ErrorMessage = "Выбрана некорректная категория.")]
        public string Category { get; set; } = "";

        [Display(Name = "Описание")]
        [Required(ErrorMessage = "Добавьте описание товара.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Описание должно быть от 10 до 1000 символов.")]
        public string Description { get; set; } = "";

        [Display(Name = "Ссылка на изображение")]
        [StringLength(500, ErrorMessage = "Ссылка или путь к изображению не должны превышать 500 символов.")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Файл изображения")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Материал")]
        [Required(ErrorMessage = "Укажите материал товара.")]
        [StringLength(100, ErrorMessage = "Материал не должен превышать 100 символов.")]
        public string Material { get; set; } = "";

        [Display(Name = "В наличии")]
        public bool InStock { get; set; } = true;
    }
}
