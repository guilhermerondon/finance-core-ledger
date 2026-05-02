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
    }
}
