namespace MirSuvenirov.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public string Category { get; set; }
        public string Material { get; set; }
        public bool InStock { get; set; }
        public string Image { get; set; }
    }
}