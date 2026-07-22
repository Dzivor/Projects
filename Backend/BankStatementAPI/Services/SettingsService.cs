using BankStatementAPI.Data;
using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BankStatementAPI.Services
{
    public class SettingsService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(AppDbContext context, IConfiguration config, ILogger<SettingsService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<List<AppSettingDTO>> GetAllSettings()
        {
            var settings = await _context.AppSettings
                .AsNoTracking()
                .OrderBy(setting => setting.Key)
                .ToListAsync();

            return settings.Select(MapToDto).ToList();
        }

        public async Task<AppSettingDTO?> GetSettingByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            string trimmedKey = key.Trim();

            var setting = await _context.AppSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Key.ToLower() == trimmedKey.ToLower());

            return setting == null ? null : MapToDto(setting);
        }

        public async Task<string> GetSettingValue(string key, string defaultValue)
        {
            try
            {
                var setting = await _context.AppSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Key.ToLower() == key.Trim().ToLower());

                return setting?.Value ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async Task<decimal> GetDecimalSetting(string key, decimal defaultValue)
        {
            string value = await GetSettingValue(key, defaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return decimal.TryParse(value, out decimal result) ? result : defaultValue;
        }

        public async Task<(bool Success, string Message, AppSettingDTO? Setting)> UpdateSetting(
            string key,
            UpdateSettingRequestDTO request,
            string adminUsername)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return (false, "Setting not found", null);
            }

            var setting = await _context.AppSettings
                .FirstOrDefaultAsync(item => item.Key.ToLower() == key.Trim().ToLower());

            if (setting == null)
            {
                return (false, "Setting not found", null);
            }

            string newValue = request.Value.Trim();
            string validationMessage = ValidateSettingValue(setting.DataType, newValue);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                return (false, $"Invalid value: {validationMessage}", null);
            }

            string oldValue = setting.Value;
            string reason = request.Reason?.Trim() ?? string.Empty;

            setting.Value = newValue;
            setting.LastUpdatedAt = DateTime.UtcNow;
            setting.LastUpdatedBy = adminUsername;

            var auditLog = new SettingsAuditLog
            {
                SettingKey = setting.Key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = adminUsername,
                ChangedAt = DateTime.UtcNow,
                Reason = string.IsNullOrWhiteSpace(reason) ? null : reason,
                AppSettingId = setting.Id
            };

            _context.SettingsAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Admin {AdminUsername} changed {Key} from {OldValue} to {NewValue}. Reason: {Reason}",
                adminUsername,
                setting.Key,
                oldValue,
                newValue,
                string.IsNullOrWhiteSpace(reason) ? "not provided" : reason);

            return (true, "Setting updated", MapToDto(setting));
        }

        public async Task<List<SettingsAuditLogDTO>> GetSettingsHistory()
        {
            var logs = await _context.SettingsAuditLogs
                .AsNoTracking()
                .OrderByDescending(log => log.ChangedAt)
                .ToListAsync();

            return logs.Select(log => new SettingsAuditLogDTO
            {
                Id = log.Id,
                SettingKey = log.SettingKey,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                ChangedBy = log.ChangedBy,
                ChangedAt = log.ChangedAt,
                Reason = log.Reason
            }).ToList();
        }

        public async Task SeedDefaultSettings(IConfiguration config)
        {
            var defaultSettings = new List<AppSetting>
            {
                new AppSetting
                {
                    Key = "VisaChargePerPage",
                    Value = config["Charging:VisaChargePerPage"] ?? "12.00",
                    Description = "Charge per page for VISA statement printing",
                    DataType = "decimal"
                },
                new AppSetting
                {
                    Key = "ChargeCollectionAccount",
                    Value = config["BankApi:ChargeCollectionAccount"] ?? "",
                    Description = "Bank account that receives VISA charges",
                    DataType = "string"
                },
                new AppSetting
                {
                    Key = "StatementMaxDateRangeDays",
                    Value = "365",
                    Description = "Maximum allowed days between statement start and end date",
                    DataType = "int"
                },
                new AppSetting
                {
                    Key = "SessionTimeoutMinutes",
                    Value = "30",
                    Description = "JWT token expiry time in minutes",
                    DataType = "int"
                },
                new AppSetting
                {
                    Key = "BankApi:Username",
                    Value = config["BankApi:Username"] ?? "",
                    Description = "Bank API username for authentication",
                    DataType = "string"
                },
                new AppSetting
                {
                    Key = "BankApi:Password",
                    Value = config["BankApi:Password"] ?? "",
                    Description = "Bank API password for authentication",
                    DataType = "password"
                },
                new AppSetting
                {
                    Key = "BankApi:SignOn",
                    Value = config["BankApi:SignOn"] ?? "",
                    Description = "Bank API SignOn (T24 sign-on) sent in the credentials header",
                    DataType = "string"
                }
            };

            var existingKeys = await _context.AppSettings
                .AsNoTracking()
                .Select(setting => setting.Key.ToLower())
                .ToListAsync();

            var settingsToAdd = defaultSettings
                .Where(setting => !existingKeys.Contains(setting.Key.ToLower()))
                .ToList();

            if (settingsToAdd.Count == 0)
            {
                return;
            }

            await _context.AppSettings.AddRangeAsync(settingsToAdd);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} default settings", settingsToAdd.Count);
        }

        private static AppSettingDTO MapToDto(AppSetting setting)
        {
            return new AppSettingDTO
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                DataType = setting.DataType,
                LastUpdatedAt = setting.LastUpdatedAt,
                LastUpdatedBy = setting.LastUpdatedBy
            };
        }

        private static string ValidateSettingValue(string dataType, string value)
        {
            return dataType.ToLower() switch
            {
                "decimal" when !decimal.TryParse(value, out decimal decimalValue) || decimalValue <= 0 => "must be a decimal greater than 0",
                "int" when !int.TryParse(value, out int intValue) || intValue <= 0 => "must be an integer greater than 0",
                "string" when string.IsNullOrWhiteSpace(value) => "must not be empty or whitespace",
                "decimal" or "int" or "string" => string.Empty,
                _ => string.Empty
            };
        }
    }
}