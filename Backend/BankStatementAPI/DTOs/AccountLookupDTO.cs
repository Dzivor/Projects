namespace BankStatementAPI.DTOs
{
    // DTO for account lookup results
    public class AccountLookupDTO
    {
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
    }

    public class AccountLookupResultDTO
    {
        public bool Success { get; set; }
        public bool AccountNotFound { get; set; }
        public string Message { get; set; } = "";
        public string? ErrorCode { get; set; }
        public string? SelectedChannel { get; set; }
        public string? SuggestedChannel { get; set; }
        public AccountLookupDTO? Account { get; set; }
    }
}