using BankStatementAPI.DTOs;
using BankStatementAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankStatementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly SettingsService _settingsService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AdminService adminService, SettingsService settingsService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _adminService.GetDashboardStats();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetStats), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading dashboard stats." });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? status)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _adminService.GetAllUsers(search, status);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetUsers), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading users." });
            }
        }

        [HttpGet("users/ad-lookup/{username}")]
        public IActionResult LookupUserInAd(string username)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Username is required" });
            }

            var result = _adminService.LookupUserInAD(username.Trim());
            return Ok(result);
        }

        [HttpPost("users")]
        public async Task<IActionResult> AddUser([FromBody] AddUserRequestDTO request)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            if (request is null || string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { message = "Username is required" });
            }

            try
            {
                var result = await _adminService.AddUser(request, GetAdminUsername());

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(AddUser), ex.Message);
                return StatusCode(500, new { message = "An error occurred while adding the user." });
            }
        }

        [HttpPut("users/{id}/toggle")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _adminService.ToggleUserStatus(id, GetAdminUsername());

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(result.User);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(ToggleUserStatus), ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating the user." });
            }
        }

        [HttpGet("audit-logs/{id:int}")]
        public async Task<IActionResult> GetAuditLogById(int id)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _adminService.GetAuditLogDrillDown(id);
                if (result == null)
                {
                    return NotFound(new { message = "Audit log not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetAuditLogById), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading audit log details." });
            }
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? staffUsername,
            [FromQuery] string? channel,
            [FromQuery] string? accountNumber)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var filter = BuildAuditLogFilter(startDate, endDate, staffUsername, channel, accountNumber);
                var result = await _adminService.GetAuditLogs(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetAuditLogs), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading audit logs." });
            }
        }


        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _settingsService.GetAllSettings();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetSettings), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading settings." });
            }
        }

        [HttpGet("settings/history")]
        public async Task<IActionResult> GetSettingsHistory()
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var result = await _settingsService.GetSettingsHistory();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(GetSettingsHistory), ex.Message);
                return StatusCode(500, new { message = "An error occurred while loading settings history." });
            }
        }

        [HttpPut("settings/{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequestDTO request)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new { message = "Setting key is required" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Value))
            {
                return BadRequest(new { message = "Setting value is required" });
            }

            try
            {
                var result = await _settingsService.UpdateSetting(key, request, GetAdminUsername());

                if (!result.Success)
                {
                    return BadRequest(new { message = result.Message });
                }

                return Ok(result.Setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(UpdateSetting), ex.Message);
                return StatusCode(500, new { message = "An error occurred while updating the setting." });
            }
        }

        [HttpGet("audit-logs/export/excel")]
        public async Task<IActionResult> ExportAuditLogsToExcel(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? staffUsername,
            [FromQuery] string? channel,
            [FromQuery] string? accountNumber)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var filter = BuildAuditLogFilter(startDate, endDate, staffUsername, channel, accountNumber);
                byte[] fileBytes = await _adminService.ExportAuditLogsToExcel(filter);
                string fileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                _logger.LogInformation("Admin {Username} exported audit logs to Excel", GetAdminUsername());

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(ExportAuditLogsToExcel), ex.Message);
                return StatusCode(500, new { message = "An error occurred while exporting audit logs to Excel." });
            }
        }

        [HttpGet("audit-logs/export/pdf")]
        public async Task<IActionResult> ExportAuditLogsToPdf(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? staffUsername,
            [FromQuery] string? channel,
            [FromQuery] string? accountNumber)
        {
            var denied = CheckAdminAccess();
            if (denied != null) return denied;

            try
            {
                var filter = BuildAuditLogFilter(startDate, endDate, staffUsername, channel, accountNumber);
                string adminName = GetAdminUsername();
                byte[] fileBytes = await _adminService.ExportAuditLogsToPdf(filter, adminName);
                string fileName = $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

                _logger.LogInformation("Admin {Username} exported audit logs to PDF", adminName);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminController.{MethodName}: {Message}", nameof(ExportAuditLogsToPdf), ex.Message);
                return StatusCode(500, new { message = "An error occurred while exporting audit logs to PDF." });
            }
        }

        private string GetAdminUsername() => User.FindFirstValue(ClaimTypes.Name) ?? "";

        private bool IsAdmin() => User.FindFirstValue("isAdmin") == "true";

        private IActionResult? CheckAdminAccess()
        {
            if (!IsAdmin())
            {
                _logger.LogWarning(
                    "Non-admin {Username} attempted admin access: {Path}",
                    GetAdminUsername(),
                    HttpContext.Request.Path);

                return StatusCode(403, new
                {
                    message = "Access denied. Admin privileges required."
                });
            }

            return null;
        }

        private static AuditLogFilterDTO BuildAuditLogFilter(
            DateTime? startDate,
            DateTime? endDate,
            string? staffUsername,
            string? channel,
            string? accountNumber)
        {
            return new AuditLogFilterDTO
            {
                StartDate = startDate,
                EndDate = endDate,
                StaffUsername = staffUsername,
                Channel = channel,
                AccountNumber = accountNumber
            };
        }
    }
}