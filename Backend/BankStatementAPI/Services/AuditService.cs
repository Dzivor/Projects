using BankStatementAPI.Models;
using BankStatementAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace BankStatementAPI.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        // Logs a statement generation to the database
        public async Task LogStatement(
            int userId,
            string accountNumber,
            string accountHolderName,
            DateTime startDate,
            DateTime endDate,
            string channelUsed,
            ChargingResult chargingResult)
        {
            var log = new AuditLog
            {
                UserId = userId,
                AccountNumber = accountNumber,
                AccountHolderName = accountHolderName,

                // Convert DateTime to DateOnly
                // since AuditLog uses DateOnly for dates
                StartDate = DateOnly.FromDateTime(startDate),
                EndDate = DateOnly.FromDateTime(endDate),

                ChannelUsed = channelUsed,
                NumberOfPages = chargingResult.NumberOfPages,
                AmountCharged = chargingResult.TotalCharge,
                AccountCharged = chargingResult.AccountCharged ?? "",
                WasWaived = chargingResult.Status == ChargeStatus.Waived,
                GeneratedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // Returns all audit logs ordered by most recent first
        public async Task<List<AuditLog>> GetAllLogs()
        {
            return await _context.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(l => l.GeneratedAt)
                .ToListAsync();
        }
    }
}