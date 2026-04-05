namespace MirSuvenirov.Models
{
    public class ProductFilterViewModel
    {
        public List<Product> Products { get; set; }
        public string Query { get; set; }
        public string Category { get; set; }
        public string Material { get; set; }
        public string Availability { get; set; }
        public string Sort { get; set; }
        public int? PriceMin { get; set; }
        public int? PriceMax { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Materials { get; set; }
    }
}