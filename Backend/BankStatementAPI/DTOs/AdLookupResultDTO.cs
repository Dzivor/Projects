namespace BankStatementAPI.DTOs
{
    public class AdLookupResultDTO
    {
        public bool Found { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Message { get; set; }
    }
}