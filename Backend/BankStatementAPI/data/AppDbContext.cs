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
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<SettingsAuditLog> SettingsAuditLogs { get; set; }

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

            modelBuilder.Entity<AppSetting>(entity =>
            {
                entity.HasIndex(s => s.Key).IsUnique();
                entity.HasMany(s => s.AuditLogs)
                      .WithOne(a => a.AppSetting)
                      .HasForeignKey(a => a.AppSettingId);
            });

            //Configure User table
            modelBuilder.Entity<User>(entity =>
            {
                //Username being unique
                entity.HasIndex(u => u.Username).IsUnique();

                entity.HasData(new User
                {
                    Id = 1,
                    Username = "Daniel.Dzivor",
                    FullName = "Daniel Dzivor",
                    Email = "Daniel.Dzivor@myumbbank.com",
                    IsActive = true,
                    IsAdmin = false,
                    CreatedAt = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                    AddedBy = "Daniel"
                });
            });
        }
    }
}
