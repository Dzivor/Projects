namespace BankStatementAPI.Models
{
    public class AppSetting
    {
        public int Id { get; set; }

        // Unique key e.g. "VisaChargePerPage"
        public string Key { get; set; } = "";

        // Value stored as string e.g. "12.00"
        public string Value { get; set; } = "";

        // Human readable description
        public string Description { get; set; } = "";

        // "decimal", "string", "int"
        public string DataType { get; set; } = "string";

        public DateTime LastUpdatedAt { get; set; }
            = DateTime.UtcNow;

        public string LastUpdatedBy { get; set; } = "";

        // Navigation property
        public List<SettingsAuditLog> AuditLogs { get; set; }
            = new();
    }
}