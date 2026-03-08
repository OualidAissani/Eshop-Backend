using Eshop.Payment.Models;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Payment.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Webhook> WebhookLog { get; set; }
        public DbSet<Models.Payment> Payments { get; set; }
    }
}
