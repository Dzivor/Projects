namespace BankStatementAPI.Models
{
    public class Statement
    {
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string Branch { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string ResidentialAddress { get; set; } = "";
        public string StreetAddress { get; set; } = "";
        public string PostalAddress { get; set; } = "";
        public string BranchAddress { get; set; } = "";
        public string Channel { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal BookBalance { get; set; }
        public decimal ClearBalance { get; set; }
        public decimal TotalDebitValue { get; set; }
        public decimal TotalCreditValue { get; set; }
        public int TotalDebitCount { get; set; }
        public int TotalCreditCount { get; set; }
        public List<Transaction> Transactions { get; set; } = new();
    }
}