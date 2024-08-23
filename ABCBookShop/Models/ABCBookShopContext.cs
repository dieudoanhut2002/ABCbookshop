using ABCBookShop.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace ABCBookShop.Models
{
    public class ABCBookShopContext : DbContext
    {
        public ABCBookShopContext(DbContextOptions<ABCBookShopContext> options) : base(options)
        {
        }
        public DbSet<Book> Books { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Discount> Discounts { get; set; }
        public DbSet<Price> Prices { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<ShelvesLocation> ShelvesLocations { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Store> Stores { get; set; }
    }
}
