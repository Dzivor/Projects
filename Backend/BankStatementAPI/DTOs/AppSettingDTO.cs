namespace BankStatementAPI.DTOs
{
    public class AppSettingDTO
    {
        public int Id { get; set; }
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";
        public string DataType { get; set; } = "";
        public DateTime LastUpdatedAt { get; set; }
        public string LastUpdatedBy { get; set; } = "";
    }

    public class UpdateSettingRequestDTO
    {
        public string Value { get; set; } = "";
        public string? Reason { get; set; }
    }

    public class SettingsAuditLogDTO
    {
        public int Id { get; set; }
        public string SettingKey { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public string ChangedBy { get; set; } = "";
        public DateTime ChangedAt { get; set; }
        public string? Reason { get; set; }
    }
}