namespace BankStatementAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string  AddedBy { get; set; } = "";

        public List<AuditLog> AuditLogs { get; set;} = new ();
    }
}