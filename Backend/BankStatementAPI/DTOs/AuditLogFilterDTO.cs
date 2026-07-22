namespace BankStatementAPI.DTOs
{
    public class AuditLogFilterDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? StaffUsername { get; set; }
        public string? Channel { get; set; }
        public string? AccountNumber { get; set; }
    }
}