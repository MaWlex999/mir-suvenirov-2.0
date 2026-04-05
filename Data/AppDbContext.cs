using Microsoft.EntityFrameworkCore;
using MirSuvenirov.Models;

namespace MirSuvenirov.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<BasketItem> Baskets { get; set; }
        public DbSet<FavoriteItem> Favorites { get; set; }
    }
}
