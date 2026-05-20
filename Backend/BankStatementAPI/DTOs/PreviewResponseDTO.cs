namespace BankStatementAPI.DTOs
{
    public class PreviewResponseDTO
    {
        public string PreviewToken { get; set; } = "";
        public int NumberOfPages { get; set; }
        public decimal TotalCharge { get; set; }
        public string? AccountToCharge { get; set; }
        public string ChargeMessage { get; set; } = "";

        // When the preview involves charging a (possibly different) account
        // these fields describe the account that will be debited.
        public string? AccountToChargeName { get; set; }
        public decimal AccountToChargeBalance { get; set; }

        public string ResidentialAddress { get; set; } = "";

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