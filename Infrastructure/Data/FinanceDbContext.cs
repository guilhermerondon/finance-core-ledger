using FinanceAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceAPI.Infrastructure.Data
{
    public class FinanceDbContext : IdentityDbContext<IdentityUser>
    {
        public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ClickLog> ClickLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<IdentityUser>(entity => {
                entity.Property(e => e.EmailConfirmed).HasColumnType("boolean");
                entity.Property(e => e.PhoneNumberConfirmed).HasColumnType("boolean");
                entity.Property(e => e.TwoFactorEnabled).HasColumnType("boolean");
                entity.Property(e => e.LockoutEnabled).HasColumnType("boolean");
            });
        }
    }
}
