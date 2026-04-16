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

        public async Task LogStatement(
            string staffUsername,
            string staffFullName,
            string accountNumber,
            string accountHolderName,
            DateTime startDate,
            DateTime endDate,
            string channelUsed,
            string staffId,
            ChargingResult ChargingResult
            
        )
        {
            var log = new AuditLog
            {
                StaffUsername = staffUsername,
                StaffFullName = staffFullName,
                StaffId = staffId,
                AccountNumber = accountNumber,
                AccountHolderName = accountHolderName,
                StartDate = startDate,
                EndDate = endDate,
                ChannelUsed = channelUsed,
                NumberOfPages = ChargingResult.NumberOfPages,
                AmountCharged = ChargingResult.TotalCharge,
                AccountCharged = ChargingResult.AccountCharged,
                WasWaived = ChargingResult.Status == ChargeStatus.Waived,
                GeneratedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        //Returning all audit logs

       public async Task<List<AuditLog>> GetAllLogs()
        {
            return await _context.AuditLogs.OrderByDescending(l=>l.GeneratedAt).ToListAsync();
        }
    }
}