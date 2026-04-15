using System.Runtime.CompilerServices;

namespace BankStatementAPI.Models
{
    // Represents the full statement returned by the bank API
    public class Statement
    {
        public string AccountName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string PostalAddress { get; set; } = "";

        public string HouseAddress { get; set; } = "";
        public string Branch {get; set;}="";
        
         public string PrintedOn { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string Currency { get; set; } = "";
        public List<Transaction> Transactions { get; set; } = new();
        public decimal BookBalance { get; set; }
        public decimal ClearBalance {get; set;}
        public decimal TotalDebitValue {get; set;}
        public decimal TotalCreditValue {get; set;}
        public int TotalCreditNumber {get; set;}
        public int TotalDebitNumber {get; set;}
        public decimal ClosingBalance { get; set; }
    }
}