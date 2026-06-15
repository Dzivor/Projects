using BankStatementAPI.Models;

using BankStatementAPI.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
        public async Task<int> LogStatement(
            int userId,
            string accountNumber,
            string accountHolderName,
            DateTime startDate,
            DateTime endDate,
            string channelUsed,
            ChargingResult chargingResult)
        {
            try
            {
                var log = new AuditLog
                {
                    UserId = userId,
                    AccountNumber = accountNumber,
                    AccountHolderName = accountHolderName,

                    StartDate = DateOnly.FromDateTime(startDate),
                    EndDate = DateOnly.FromDateTime(endDate),

                    ChannelUsed = channelUsed,
                    NumberOfPages = chargingResult.NumberOfPages,
                    AmountCharged = chargingResult.TotalCharge,
                    AccountCharged = chargingResult.AccountCharged ?? "",
                    WasWaived = chargingResult.Status == ChargeStatus.Waived,
                    BankTransactionReference = chargingResult.BankTransactionReference,
                    GeneratedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();

                // Link ChargeTransaction to this AuditLog
                if (chargingResult.ChargeTransactionId.HasValue)
                {
                    var chargeLog = await _context.ChargeTransactions
                        .FirstOrDefaultAsync(c => c.Id == chargingResult.ChargeTransactionId.Value);

                    if (chargeLog != null)
                    {
                        chargeLog.AuditLogId = log.Id;
                        await _context.SaveChangesAsync();
                    }
                }

                return log.Id;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to log AuditLog for account {AccountNumber}", accountNumber);

                Serilog.Log.Error(ex, "Failed to log AuditLog for account {AccountNumber}", accountNumber);

                throw;
            }
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