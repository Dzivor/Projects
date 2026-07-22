namespace BankStatementAPI.DTOs
{
    public class AddUserRequestDTO
    {
        public string Username { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
    }
}