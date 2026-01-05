using Eshop.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Orders.Data
{
    public class OrderDbContext:DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options):base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
                
        }
        public DbSet<Order> Orders { get; set; }


    }
}
