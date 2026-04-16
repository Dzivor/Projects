using BankStatementAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace BankStatementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AuditLog> AuditLogs { get; set; }

      //configuring table names and relationships if needed
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Configure AuditLog table
            modelBuilder.Entity<AuditLog>(entity =>
            {
                //precision for decimal fields(18 toal digits, 2 decimal places)
                entity.Property(e => e.AmountCharged).HasPrecision(18, 2);
            });
        }
    }
}
