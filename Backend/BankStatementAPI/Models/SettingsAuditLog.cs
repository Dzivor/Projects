namespace BankStatementAPI.Models
{
    public class SettingsAuditLog
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string ChangedBy { get; set; } = "";
        public DateTime ChangedAt { get; set; }
            = DateTime.UtcNow;

        // Optional reason provided by admin
        public string? Reason { get; set; }

        // FK to AppSetting
        public int AppSettingId { get; set; }
        public AppSetting? AppSetting { get; set; }
    }
}