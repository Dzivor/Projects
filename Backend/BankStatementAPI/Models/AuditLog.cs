using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankStatementAPI.Models
{
     public class AuditLog
    {
        
        public int Id {get; set;}
        public string StaffUsername{get; set;}="";
        public string StaffId{get; set;}="";
        public string StaffFullName {get; set;}="";

        
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



       

        public string? BankTransactionReference {get;set;}
        public bool WasReversed {get; set;}

        public string? ReversalReason {get; set;}

 //Timestamp
        public DateTime GeneratedAt {get; set;}=DateTime.UtcNow;
    }
}