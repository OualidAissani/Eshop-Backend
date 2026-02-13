using Eshop.Payement.Models;
using Microsoft.EntityFrameworkCore;

namespace Eshop.Payement.Data
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
        public DbSet<Payment> Payments { get; set; }
    }
}
