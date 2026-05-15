using System.Text.Json.Serialization;

namespace BankStatementAPI.Models
{
    // ─────────────────────────────────────────
    // ACCOUNT INFO RESPONSE
    // Maps: /party/umbGetAcctInfo/
    // ─────────────────────────────────────────

    public class BankApiAccountInfo
    {
        public string AccountStatus { get; set; } = "";
        [JsonPropertyName("fullName")]
        public string Name { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string Currency { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountBalance { get; set; } = "";
        public string SuccessIndicator { get; set; } = "";
        public string CustomerID { get; set; } = "";
        public string Country { get; set; } = "";
    }

    public class BankApiAccountResponseHeader
    {
        public string Status { get; set; } = "";
    }

    public class BankApiAccountResponse
    {
        public BankApiAccountResponseHeader Header { get; set; } = new();
        public List<BankApiAccountInfo> Body { get; set; } = new();
    }

    // ─────────────────────────────────────────
    // STATEMENT RESPONSE
    // Maps: /party/account/getAccountStatements.2.1.0
    // ─────────────────────────────────────────

    public class StatementHeaderData
    {
        public string TodayDate { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountTitle { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string Currency { get; set; } = "";
        public string Branch { get; set; } = "";
        public string ResidentialAddress { get; set; } = "";
        public string Street { get; set; } = "";
        public string PostalAddress { get; set; } = "";
        public string OpeningBalance { get; set; } = "";
        public string TotalDebit { get; set; } = "";
        public string TotalCredit { get; set; } = "";
        public string ClearedBalance { get; set; } = "";
        public string TotalAmount { get; set; } = "";
    }

    public class StatementHeader
    {
        public string Status { get; set; } = "";
        public StatementHeaderData Data { get; set; } = new();
    }

    public class TransactionDescription
    {
        public string Description { get; set; } = "";
    }

    public class BankApiTransaction
    {
        public List<TransactionDescription> Descriptions { get; set; } = new();
        public string BookingDate { get; set; } = "";
        public string Reference { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public string ValueDate { get; set; } = "";
        public string DebitAmount { get; set; } = "";
        public string CreditAmount { get; set; } = "";
        public string ClosingBalance { get; set; } = "";
    }

    public class BankApiStatementResponse
    {
        public StatementHeader Header { get; set; } = new();
        public List<BankApiTransaction> Body { get; set; } = new();
    }

    // ─────────────────────────────────────────
    // TRANSFER RESPONSE
    // Maps: /party/payments/createGenericTransfer
    // ─────────────────────────────────────────

    public class TransferResponseHeader
    {
        public string Status { get; set; } = "";
        public string TransactionStatus { get; set; } = "";
        public string Id { get; set; } = "";
    }

    public class BankApiTransferErrorDetail
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class BankApiTransferError
    {
        public string Type { get; set; } = "";
        public List<BankApiTransferErrorDetail> ErrorDetails { get; set; } = new();
    }

    public class BankApiTransferResponse
    {
        public TransferResponseHeader Header { get; set; } = new();
        public BankApiTransferError? Error { get; set; }
    }
}