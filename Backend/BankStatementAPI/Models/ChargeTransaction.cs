using System.ComponentModel.DataAnnotations;

namespace BankStatementAPI.Models
{
    public enum ChargeTransactionStatus
    {
        Pending,
        Success,
        Failed
    }

    public class ChargeTransaction
    {
        public int Id { get; set; }

        // The account that was debited
        public string DebitAccountNumber { get; set; } = "";

        // The account that received the charge
        public string CreditAccountNumber { get; set; } = "";

        public decimal Amount { get; set; }

        // "VISA" or "ESB"
        public string Channel { get; set; } = "";

        // The statement account this charge is for
        public string StatementAccountNumber { get; set; } = "";

        // Transaction reference from bank e.g. "FT22265SJC32"
        public string? BankTransactionReference { get; set; }

        public ChargeTransactionStatus Status { get; set; }

        // Error message if Status = Failed
        public string? ErrorMessage { get; set; }

        // Staff who triggered this charge
        public string StaffUsername { get; set; } = "";

        // Narration sent to bank
        public string Narration { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // FK to AuditLog — linked after PDF generated
        public int? AuditLogId { get; set; }

        public AuditLog? AuditLog { get; set; }
    }
}

