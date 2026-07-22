namespace BankStatementAPI.DTOs
{
    public class ChargeTransactionDTO
    {
        public int Id { get; set; }
        public string DebitAccountNumber { get; set; } = "";
        public string CreditAccountNumber { get; set; } = "";
        public decimal Amount { get; set; }
        public string Channel { get; set; } = "";
        public string StatementAccountNumber { get; set; } = "";
        public string? BankTransactionReference { get; set; }
        public string Status { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public string StaffUsername { get; set; } = "";
        public string Narration { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? AuditLogId { get; set; }
    }
}

