using System;

namespace BankStatementAPI.DTOs
{
    public class AuditLogChargeDrillDownDTO
    {
        public string DebitAccountNumber { get; set; } = "";
        public string CreditAccountNumber { get; set; } = "";
        public string StatementAccountNumber { get; set; } = "";
        public string? BankTransactionReference { get; set; }
        public string Narration { get; set; } = "";
        public DateTime? CompletedAt { get; set; }
    }

    public class AuditLogDrillDownDTO
    {
        public int Id { get; set; }

        // Statement/Audit fields
        public string StaffFullName { get; set; } = "";
        public string StaffUsername { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string AccountHolderName { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string ChannelUsed { get; set; } = "";
        public int NumberOfPages { get; set; }
        public decimal AmountCharged { get; set; }
        public string AccountCharged { get; set; } = "";
        public bool WasWaived { get; set; }
        public string? BankTransactionReference { get; set; }
        public DateTime GeneratedAt { get; set; }

        // Charge details (nullable when no linked charge exists)
        public AuditLogChargeDrillDownDTO? Charge { get; set; }

        // Message shown when Charge is null (ESB/waived)
        public string? ChargeMessage { get; set; }
    }
}

