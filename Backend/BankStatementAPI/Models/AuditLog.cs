using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BankStatementAPI.Models
{
     public class AuditLog
    {
        // Primary Key
        public int Id {get; set;}
       // Staff Information
        public string StaffUsername{get; set;}="";
        public string StaffId{get; set;}="";
        public string StaffFullName {get; set;}="";

        //Statement Request Details
        public string AccountNumber {get; set;}="";
        public string AccountHolderName{get; set;}="";
        public DateTime StartDate {get; set;}
        public DateTime EndDate {get; set;}


        //Charge Details
        public string ChannelUsed {get; set;}="";
        public int NumberOfPages {get; set;}
        public decimal AmountCharged {get; set;}
        public string AccountCharged {get; set;} = "";
        public bool WasWaived {get; set;}



        //Timestamp
        public DateTime GeneratedAt {get; set;}=DateTime.UtcNow;


    }
}