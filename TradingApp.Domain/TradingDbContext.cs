using Microsoft.EntityFrameworkCore;
using TradingApp.Domain.Models.Entities;
using TradingApp.Domain.Models.Entities.Order;
using TradingApp.Domain.Models.Entities.OutboxMessage;

namespace TradingApp.Domain
{
    public class TradingDbContext : DbContext
    {
        public TradingDbContext(DbContextOptions<TradingDbContext> options) : base(options) { }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OutboxMessage> OutboxMessages {get; set; }

        public DbSet<DeadLetterLog> DeadLetterLogs { get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClientOrderId).IsRequired();
                entity.HasIndex(e => e.ClientOrderId).IsUnique();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.IsProcessed).IsRequired();
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Payload).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.ProcessedAt);
                entity.Property(e => e.RetryCount).IsRequired();
            });

            modelBuilder.Entity<DeadLetterLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClientOrderId).IsRequired();
                entity.HasIndex(e => e.ClientOrderId);
                entity.Property(e => e.MessageBody).IsRequired();
                entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsResolved).IsRequired();
                entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
                entity.Property(e => e.ResolvedAt);
                entity.Property(e => e.ResolvedBy).HasMaxLength(200);
            });
        }
    }
}
