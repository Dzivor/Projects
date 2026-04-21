namespace BankStatementAPI.DTOs
{
    // API response after successful login
    public class LoginResponseDTO
    {
        public string Token { get; set; } = "";
        public string Username { get; set; } = "";
        public string FirstName { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}