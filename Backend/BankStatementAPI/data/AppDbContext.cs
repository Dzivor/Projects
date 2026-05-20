using BankStatementAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace BankStatementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

      //configuring table names and relationships if needed
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Configure AuditLog table
            modelBuilder.Entity<AuditLog>(entity =>
            {
                //precision for decimal fields(18 toal digits, 2 decimal places)
                entity.Property(e => e.AmountCharged).HasPrecision(18, 2);

                entity.HasOne(a => a.User).WithMany(u => u.AuditLogs).HasForeignKey(a => a.UserId);
            });

            //Configure User table
            modelBuilder.Entity<User>(entity =>
            {
                //Username being unique
                entity.HasIndex(u => u.Username).IsUnique();
            });
        }
    }
}
