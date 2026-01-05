using Microsoft.EntityFrameworkCore;

namespace Eshop.Inventory.Data
{
    public class InventoryDb:DbContext
    {
        public InventoryDb(DbContextOptions contextOptions):base(contextOptions)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Models.Inventory> Inventories { get; set; }
    }
}
