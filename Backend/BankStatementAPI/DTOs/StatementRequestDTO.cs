namespace BankStatementAPI.DTOs
{
    // request DTO for statement retrieval
    public class StatementRequestDTO
    {
        public string AccountNumber { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string Channel { get; set; } = "";      // "VISA" or "ESB"
        public bool WaiveCharge { get; set; }           // waive charge checkbox
        public bool ChargeAltAccount { get; set; }      // alt account checkbox
        public string? AltAccountNumber { get; set; }   // optional alt account
        public string? PreviewToken { get; set; }       // token from preview flow
        public int UserId { get; set; }                 // for audit logging
    }
}