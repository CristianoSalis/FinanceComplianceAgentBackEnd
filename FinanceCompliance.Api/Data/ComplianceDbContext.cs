using Microsoft.EntityFrameworkCore;
using FinanceCompliance.Api.Models;

namespace FinanceCompliance.Api.Data
{
    public class ComplianceDbContext : DbContext
    {
        public ComplianceDbContext(DbContextOptions<ComplianceDbContext> options) : base(options)
        {
        }

        public DbSet<ComplianceRule> ComplianceRules { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Mapping and explicit definition of primary keys
            modelBuilder.Entity<ComplianceRule>(entity =>
            {
                entity.ToTable("ComplianceRules");
                entity.HasKey(e => e.RuleId);
            });

            modelBuilder.Entity<FinancialTransaction>(entity =>
            {
                entity.ToTable("FinancialTransactions");
                entity.HasKey(e => e.TransactionId);
            });
        }
    }
}