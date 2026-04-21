namespace BankStatementAPI.DTOs
{
    public class PreviewResponseDTO
    {
        public int NumberOfPages { get; set; }
        public decimal TotalCharge { get; set; }
        public string? AccountToCharge { get; set; }
        public string ChargeMessage { get; set; } = "";

        // These come from the updated Statement model
        public string AccountName { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string Branch { get; set; } = "";
        public string AccountType { get; set; } = "";
        public decimal BookBalance { get; set; }
        public decimal ClearBalance { get; set; }
        public decimal TotalDebitValue { get; set; }
        public decimal TotalCreditValue { get; set; }
        public int TotalDebitCount { get; set; }
        public int TotalCreditCount { get; set; }
    }
}