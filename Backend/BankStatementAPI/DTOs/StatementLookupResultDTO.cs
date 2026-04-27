using BankStatementAPI.Models;

namespace BankStatementAPI.DTOs
{
    public class StatementLookupResultDTO
    {
        public bool Success { get; set; }
        public bool StatementNotFound { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string? SelectedChannel { get; set; }
        public string? SuggestedChannel { get; set; }
        public Statement? Statement { get; set; }
    }
}
