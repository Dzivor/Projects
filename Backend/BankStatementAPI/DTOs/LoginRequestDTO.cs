namespace BankStatementAPI.DTOs
{
    // DTO for login request
    public class LoginRequestDTO
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }
}