namespace BankStatementAPI.DTOs
{
    // DTO for account lookup results
    public class AccountLookupDTO
    {
        public string AccountNumber { get; set; } = "";
        public string AccountName { get; set; } = "";
    }
}