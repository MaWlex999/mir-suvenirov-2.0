namespace MirSuvenirov.Models
{
    public class CategoryPageViewModel
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string HeroClass { get; set; } = "";
        public List<Product> Products { get; set; } = new();
    }
}
