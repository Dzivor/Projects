using BankStatementAPI.Data;
using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.DirectoryServices.AccountManagement;

namespace BankStatementAPI.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AdminService> _logger;

        public AdminService(AppDbContext context, IConfiguration config, ILogger<AdminService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<DashboardStatsDTO> GetDashboardStats()
        {
            try
            {
                DateTime todayStart = DateTime.UtcNow.Date;
                DateTime tomorrowStart = todayStart.AddDays(1);
                DateTime firstDayOfMonth = new DateTime(todayStart.Year, todayStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var todayLogsQuery = _context.AuditLogs.AsNoTracking()
                    .Where(a => a.GeneratedAt >= todayStart && a.GeneratedAt < tomorrowStart);

                var monthLogsQuery = _context.AuditLogs.AsNoTracking()
                    .Where(a => a.GeneratedAt >= firstDayOfMonth);

                int totalUsers = await _context.Users.AsNoTracking().CountAsync();
                int activeUsers = await _context.Users.AsNoTracking().CountAsync(u => u.IsActive);
                int disabledUsers = await _context.Users.AsNoTracking().CountAsync(u => !u.IsActive);
                int statementsToday = await todayLogsQuery.CountAsync();
                int statementsTodayVisa = await todayLogsQuery.CountAsync(a => a.ChannelUsed.ToUpper() == "VISA");
                int statementsTodayEsb = await todayLogsQuery.CountAsync(a => a.ChannelUsed.ToUpper() == "ESB");
                decimal chargesToday = await todayLogsQuery.SumAsync(a => (decimal?)a.AmountCharged) ?? 0m;
                int statementsThisMonth = await monthLogsQuery.CountAsync();
                decimal chargesThisMonth = await monthLogsQuery.SumAsync(a => (decimal?)a.AmountCharged) ?? 0m;

                var monthlyLogs = await _context.AuditLogs.AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.GeneratedAt >= firstDayOfMonth)
                    .ToListAsync();

                var topStaff = monthlyLogs
                    .GroupBy(log => log.UserId)
                    .Select(group => new StaffActivityDTO
                    {
                        FullName = group.FirstOrDefault()?.User?.FullName ?? "",
                        Username = group.FirstOrDefault()?.User?.Username ?? "",
                        StatementCount = group.Count(),
                        PrimaryChannel = group
                            .GroupBy(log => log.ChannelUsed)
                            .OrderByDescending(channelGroup => channelGroup.Count())
                            .ThenBy(channelGroup => channelGroup.Key)
                            .Select(channelGroup => channelGroup.Key)
                            .FirstOrDefault() ?? "",
                        TotalCharged = group.Sum(log => log.AmountCharged)
                    })
                    .OrderByDescending(activity => activity.StatementCount)
                    .ThenBy(activity => activity.FullName)
                    .Take(5)
                    .ToList();

                return new DashboardStatsDTO
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    DisabledUsers = disabledUsers,
                    StatementsToday = statementsToday,
                    StatementsTodayVisa = statementsTodayVisa,
                    StatementsTodayEsb = statementsTodayEsb,
                    ChargesToday = chargesToday,
                    StatementsThisMonth = statementsThisMonth,
                    ChargesThisMonth = chargesThisMonth,
                    MostActiveStaff = topStaff
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(GetDashboardStats), ex.Message);
                throw;
            }
        }

        public async Task<List<AdminUserDTO>> GetAllUsers(string? search, string? status)
        {
            try
            {
                var query = _context.Users
                    .AsNoTracking()
                    .Include(user => user.AuditLogs)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string cleanSearch = search.Trim().ToLower();
                    query = query.Where(user =>
                        user.FullName.ToLower().Contains(cleanSearch) ||
                        user.Username.ToLower().Contains(cleanSearch));
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    string cleanStatus = status.Trim().ToLower();
                    if (cleanStatus == "active")
                        query = query.Where(user => user.IsActive);
                    else if (cleanStatus == "disabled")
                        query = query.Where(user => !user.IsActive);
                }

                var users = await query
                    .OrderBy(user => user.FullName)
                    .ToListAsync();

                return users.Select(user => new AdminUserDTO
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    IsAdmin = user.IsAdmin,
                    CreatedAt = user.CreatedAt,
                    AddedBy = user.AddedBy,
                    TotalStatements = user.AuditLogs.Count
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(GetAllUsers), ex.Message);
                throw;
            }
        }

        public AdLookupResultDTO LookupUserInAD(string username)
        {
            string cleanUsername = username?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(cleanUsername))
            {
                return new AdLookupResultDTO
                {
                    Found = false,
                    Message = "Username is required"
                };
            }

            try
            {
                string? domain = _config["ActiveDirectory:Domain"];

                using var context = new PrincipalContext(ContextType.Domain, domain);
                using var user = UserPrincipal.FindByIdentity(
                    context,
                    IdentityType.SamAccountName,
                    cleanUsername);

                if (user is null)
                {
                    _logger.LogWarning("AD lookup failed for username: {Username}", cleanUsername);
                    return new AdLookupResultDTO
                    {
                        Found = false,
                        Message = "User not found in Active Directory"
                    };
                }

                return new AdLookupResultDTO
                {
                    Found = true,
                    Username = cleanUsername,
                    FullName = user.DisplayName ?? cleanUsername,
                    Email = user.EmailAddress ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("AD lookup failed for username: {Username}", cleanUsername);
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(LookupUserInAD), ex.Message);
                return new AdLookupResultDTO
                {
                    Found = false,
                    Message = "Unable to reach Active Directory"
                };
            }
        }

        public async Task<(bool Success, string Message, AdminUserDTO? User)> AddUser(AddUserRequestDTO request, string adminUsername)
        {
            try
            {
                string cleanUsername = request.Username.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(cleanUsername))
                {
                    return (false, "Username is required", null);
                }

                bool alreadyExists = await _context.Users
                    .AnyAsync(user => user.Username.ToLower() == cleanUsername);

                if (alreadyExists)
                {
                    return (false, "User already exists", null);
                }

                var adUser = LookupUserInAD(cleanUsername);
                if (!adUser.Found)
                {
                    return (false, adUser.Message ?? "User not found in Active Directory", null);
                }

                var user = new User
                {
                    Username = cleanUsername,
                    FullName = adUser.FullName,
                    Email = adUser.Email,
                    IsActive = true,
                    IsAdmin = request.IsAdmin,
                    CreatedAt = DateTime.UtcNow,
                    AddedBy = adminUsername
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Admin {Username} added user {NewUser} (IsAdmin: {IsAdmin})",
                    adminUsername,
                    cleanUsername,
                    request.IsAdmin);

                return (true, "User added successfully", ToAdminUserDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(AddUser), ex.Message);
                return (false, "Unable to add user", null);
            }
        }

        public async Task<(bool Success, string Message, AdminUserDTO? User)> ToggleUserStatus(int userId, string adminUsername)
        {
            try
            {
                var user = await _context.Users
                    .Include(currentUser => currentUser.AuditLogs)
                    .FirstOrDefaultAsync(currentUser => currentUser.Id == userId);

                if (user is null)
                {
                    return (false, "User not found", null);
                }

                if (string.Equals(user.Username, adminUsername, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Cannot disable your own account", null);
                }

                if (user.IsAdmin && user.IsActive)
                {
                    int activeAdminCount = await _context.Users.CountAsync(currentUser => currentUser.IsAdmin && currentUser.IsActive);
                    if (activeAdminCount <= 1)
                    {
                        return (false, "Cannot disable the last active admin", null);
                    }
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                string action = user.IsActive ? "enabled" : "disabled";
                _logger.LogInformation("Admin {Username} {Action} user {TargetUser}", adminUsername, action, user.Username);

                return (true, $"User {action}", ToAdminUserDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(ToggleUserStatus), ex.Message);
                return (false, "Unable to update user status", null);
            }
        }

        public async Task<List<AuditLogDTO>> GetAuditLogs(AuditLogFilterDTO filter)
        {
            try
            {
                var query = _context.AuditLogs
                    .AsNoTracking()
                    .Include(auditLog => auditLog.User)
                    .AsQueryable();

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(auditLog => auditLog.GeneratedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    DateTime endDate = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(auditLog => auditLog.GeneratedAt <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(filter.StaffUsername))
                {
                    string cleanStaffUsername = filter.StaffUsername.Trim().ToLowerInvariant();
                    query = query.Where(auditLog => auditLog.User != null && auditLog.User.Username.ToLower() == cleanStaffUsername);
                }

                if (!string.IsNullOrWhiteSpace(filter.Channel))
                {
                    string cleanChannel = filter.Channel.Trim().ToUpperInvariant();
                    query = query.Where(auditLog => auditLog.ChannelUsed.ToUpper() == cleanChannel);
                }

                if (!string.IsNullOrWhiteSpace(filter.AccountNumber))
                {
                    string cleanAccountNumber = filter.AccountNumber.Trim();
                    query = query.Where(auditLog => auditLog.AccountNumber.Contains(cleanAccountNumber));
                }

                var logs = await query
                    .OrderByDescending(auditLog => auditLog.GeneratedAt)
                    .ToListAsync();

                return logs.Select(log => new AuditLogDTO
                {
                    Id = log.Id,
                    StaffFullName = log.User?.FullName ?? "",
                    StaffUsername = log.User?.Username ?? "",
                    AccountNumber = log.AccountNumber,
                    AccountHolderName = log.AccountHolderName,
                    StartDate = log.StartDate,
                    EndDate = log.EndDate,
                    ChannelUsed = log.ChannelUsed,
                    NumberOfPages = log.NumberOfPages,
                    AmountCharged = log.AmountCharged,
                    AccountCharged = log.AccountCharged,
                    WasWaived = log.WasWaived,
                    GeneratedAt = log.GeneratedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(GetAuditLogs), ex.Message);
                throw;
            }
        }

        public async Task<byte[]> ExportAuditLogsToExcel(AuditLogFilterDTO filter)
        {
            try
            {
                var logs = await GetAuditLogs(filter);

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Audit Logs");

                string[] headers =
                {
                    "Date",
                    "Staff Name",
                    "Account Number",
                    "Account Holder",
                    "Channel",
                    "Pages",
                    "Charge Amount",
                    "Account Charged",
                    "Waived"
                };

                for (int column = 0; column < headers.Length; column++)
                {
                    var cell = worksheet.Cell(1, column + 1);
                    cell.Value = headers[column];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E6A817");
                    cell.Style.Font.FontColor = XLColor.FromHtml("#1a1000");
                }

                int rowIndex = 2;
                foreach (var log in logs)
                {
                    bool isEvenRow = rowIndex % 2 == 0;

                    worksheet.Cell(rowIndex, 1).Value = log.GeneratedAt.ToString("dd MMM yyyy HH:mm");
                    worksheet.Cell(rowIndex, 2).Value = log.StaffFullName;
                    worksheet.Cell(rowIndex, 3).Value = log.AccountNumber;
                    worksheet.Cell(rowIndex, 4).Value = log.AccountHolderName;
                    worksheet.Cell(rowIndex, 5).Value = log.ChannelUsed;
                    worksheet.Cell(rowIndex, 6).Value = log.NumberOfPages;
                    worksheet.Cell(rowIndex, 7).Value = log.AmountCharged;
                    worksheet.Cell(rowIndex, 8).Value = log.AccountCharged;
                    worksheet.Cell(rowIndex, 9).Value = log.WasWaived ? "Yes" : "No";

                    if (isEvenRow)
                    {
                        worksheet.Range(rowIndex, 1, rowIndex, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#FAFAFA");
                    }

                    rowIndex++;
                }

                int summaryRow = rowIndex;
                worksheet.Cell(summaryRow, 1).Value = "TOTAL";
                worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
                worksheet.Cell(summaryRow, 6).Value = logs.Sum(log => log.NumberOfPages);
                worksheet.Cell(summaryRow, 6).Style.Font.Bold = true;
                worksheet.Cell(summaryRow, 7).Value = logs.Sum(log => log.AmountCharged);
                worksheet.Cell(summaryRow, 7).Style.Font.Bold = true;

                worksheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(ExportAuditLogsToExcel), ex.Message);
                throw;
            }
        }

        public async Task<byte[]> ExportAuditLogsToPdf(AuditLogFilterDTO filter, string adminName)
        {
            try
            {
                var logs = await GetAuditLogs(filter);
                decimal totalCharges = logs.Sum(log => log.AmountCharged);
                DateTime generatedAt = DateTime.Now;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(30);
                        page.DefaultTextStyle(textStyle => textStyle.FontSize(10).FontFamily(Fonts.Verdana));

                        page.Header().Column(header =>
                        {
                            header.Item().Row(row =>
                            {
                                row.RelativeItem().Text("UMB BANK").FontSize(18).Bold().FontColor("#E6A817");
                                row.RelativeItem().AlignRight().Text("Audit Log Report").FontSize(13).Bold();
                            });

                            header.Item().PaddingTop(4).BorderBottom(1).BorderColor("#E6A817");

                            header.Item().PaddingTop(8).Text($"Generated by: {adminName}    Date: {generatedAt:dd MMM yyyy HH:mm}    Total records: {logs.Count}");
                        });

                        page.Content().PaddingTop(12).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                AddPdfHeaderCell(header.Cell(), "Date");
                                AddPdfHeaderCell(header.Cell(), "Staff");
                                AddPdfHeaderCell(header.Cell(), "Account No");
                                AddPdfHeaderCell(header.Cell(), "Account Holder");
                                AddPdfHeaderCell(header.Cell(), "Channel");
                                AddPdfHeaderCell(header.Cell(), "Pages");
                                AddPdfHeaderCell(header.Cell(), "Charge");
                                AddPdfHeaderCell(header.Cell(), "Waived");
                            });

                            foreach (var log in logs)
                            {
                                string chargeText = log.WasWaived
                                    ? "Waived"
                                    : string.Equals(log.ChannelUsed, "ESB", StringComparison.OrdinalIgnoreCase)
                                        ? "Free"
                                        : $"GHS {log.AmountCharged:N2}";

                                AddPdfBodyCell(table.Cell(), log.GeneratedAt.ToString("dd MMM yyyy HH:mm"));
                                AddPdfBodyCell(table.Cell(), log.StaffFullName);
                                AddPdfBodyCell(table.Cell(), log.AccountNumber);
                                AddPdfBodyCell(table.Cell(), log.AccountHolderName);
                                AddPdfBodyCell(table.Cell(), log.ChannelUsed);
                                AddPdfBodyCell(table.Cell(), log.NumberOfPages.ToString());
                                AddPdfBodyCell(table.Cell(), chargeText);
                                AddPdfBodyCell(table.Cell(), log.WasWaived ? "Yes" : "No");
                            }
                        });

                        page.Footer().PaddingTop(10).Row(footer =>
                        {
                            footer.RelativeItem().Text($"Total charges: GHS {totalCharges:N2}").Bold();
                            footer.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                        });
                    });
                });

                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminService.{MethodName}: {Message}", nameof(ExportAuditLogsToPdf), ex.Message);
                throw;
            }
        }

        private static AdminUserDTO ToAdminUserDto(User user)
        {
            return new AdminUserDTO
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                IsAdmin = user.IsAdmin,
                CreatedAt = user.CreatedAt,
                AddedBy = user.AddedBy,
                TotalStatements = user.AuditLogs?.Count ?? 0
            };
        }

        private static void AddPdfHeaderCell(IContainer cell, string text)
        {
            cell.Background("#E6A817").Padding(4).Text(text).Bold().FontColor("#1a1000");
        }

        private static void AddPdfBodyCell(IContainer cell, string text)
        {
            cell.BorderBottom(0.5f).Padding(4).Text(text);
        }
    }
}