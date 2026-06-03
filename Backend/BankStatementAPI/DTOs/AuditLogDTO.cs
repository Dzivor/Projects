namespace BankStatementAPI.DTOs
{
    public class AuditLogDTO
    {
        public int Id { get; set; }
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
        public DateTime GeneratedAt { get; set; }
    }
}