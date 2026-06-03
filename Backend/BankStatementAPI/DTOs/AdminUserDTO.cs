namespace BankStatementAPI.DTOs
{
    public class AdminUserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AddedBy { get; set; } = "";
        public int TotalStatements { get; set; }
    }
}