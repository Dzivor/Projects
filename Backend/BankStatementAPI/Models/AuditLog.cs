using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankStatementAPI.Models
{
     public class AuditLog
    {
        
        public int Id {get; set;}
        
        
        // Foreign key- links to Users table
        public int UserId {get; set;}
        public User? User {get; set;} 

        //Request details
        public string AccountNumber {get; set;}="";
        public string AccountHolderName{get; set;}="";
        public DateOnly StartDate {get; set;}
        public DateOnly EndDate {get; set;}


        //Charge Details
        public string ChannelUsed {get; set;}="";
        public int NumberOfPages {get; set;}
        public decimal AmountCharged {get; set;}
        public string AccountCharged {get; set;} = "";
        public bool WasWaived {get; set;}

        // Bank transaction reference returned by debit endpoint
        // e.g. "FT22265SJC32" — null if ESB or waived
        public string? BankTransactionReference { get; set; }

    

 //Timestamp
        public DateTime GeneratedAt {get; set;}=DateTime.UtcNow;

        // Linked charge transactions (created during statement generation)
        public List<ChargeTransaction> ChargeTransactions { get; set; } = new();
    }
}
