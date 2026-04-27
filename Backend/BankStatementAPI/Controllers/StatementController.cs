using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using BankStatementAPI.Services;
using System.Globalization;
using System.Security.Claims;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BankStatementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatementController : ControllerBase
    {
        private readonly BankApiService _bankApiService;
        private readonly ChargingService _chargingService;
        private readonly PdfService _pdfService;
        private readonly AuditService _auditService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<StatementController> _logger;

        private const string PreviewCachePrefix = "statement-preview:";

        public StatementController(
            BankApiService bankApiService,
            ChargingService chargingService,
            PdfService pdfService,
            AuditService auditService,
            IMemoryCache memoryCache,
            ILogger<StatementController> logger)
        {
            _bankApiService = bankApiService;
            _chargingService = chargingService;
            _pdfService = pdfService;
            _auditService = auditService;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        // POST /api/statement/preview
        // Shows charge info before actually generating the PDF
        [HttpPost("preview")]
        public async Task<IActionResult> Preview(
            [FromBody] StatementRequestDTO request)
        {
            var totalStopwatch = Stopwatch.StartNew();

            // Step 1 — Validate request
            var validation = ValidateRequest(request);
            if (validation != null) return validation;

            // Step 2 — Parse dates
            var parseResult = TryParseDateRange(request.StartDate, request.EndDate);
            if (!parseResult.Success)
                return BadRequest(new { message = parseResult.Message });

            DateTime startDate = parseResult.StartDate;
            DateTime endDate = parseResult.EndDate;

            // Step 3 — Fetch statement from bank API
            var bankFetchStopwatch = Stopwatch.StartNew();
            var statementResult = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate, request.Channel
            );
            bankFetchStopwatch.Stop();
            _logger.LogInformation(
                "Statement preview bank fetch completed in {ElapsedMs} ms for account {AccountNumber}",
                bankFetchStopwatch.ElapsedMilliseconds,
                request.AccountNumber);

            if (!statementResult.Success)
            {
                _logger.LogWarning(
                    "Statement preview failed after {ElapsedMs} ms for account {AccountNumber}",
                    totalStopwatch.ElapsedMilliseconds,
                    request.AccountNumber);

                if (statementResult.StatementNotFound)
                    return NotFound(new
                    {
                        message = statementResult.Message,
                        code = statementResult.ErrorCode,
                        selectedChannel = statementResult.SelectedChannel,
                        suggestedChannel = statementResult.SuggestedChannel
                    });

                return StatusCode(503, new { message = statementResult.Message });
            }

            var statement = statementResult.Statement!;
            statement.Channel = request.Channel;
            statement.StartDate = DateOnly.FromDateTime(startDate);
            statement.EndDate = DateOnly.FromDateTime(endDate);

            // Step 4 — Build a rendered preview and derive pages from actual PDF layout.
            int numberOfPages = 1;
            ChargingResult chargePreview = _chargingService.PreviewCharge(request, numberOfPages);
            byte[] previewPdf = Array.Empty<byte>();
            var renderLoopStopwatch = Stopwatch.StartNew();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var singleRenderStopwatch = Stopwatch.StartNew();
                chargePreview = _chargingService.PreviewCharge(request, numberOfPages);
                previewPdf = _pdfService.GenerateStatement(statement, chargePreview);

                int renderedPages = _pdfService.CountPages(previewPdf);
                singleRenderStopwatch.Stop();
                _logger.LogInformation(
                    "Statement preview render attempt {Attempt} completed in {ElapsedMs} ms with {RenderedPages} pages",
                    attempt + 1,
                    singleRenderStopwatch.ElapsedMilliseconds,
                    renderedPages);

                if (renderedPages == numberOfPages)
                {
                    break;
                }

                numberOfPages = renderedPages;
            }

            // Finalize preview details from a stable rendered page count.
            var finalRenderStopwatch = Stopwatch.StartNew();
            chargePreview = _chargingService.PreviewCharge(request, numberOfPages);
            previewPdf = _pdfService.GenerateStatement(statement, chargePreview);
            numberOfPages = _pdfService.CountPages(previewPdf);
            finalRenderStopwatch.Stop();
            renderLoopStopwatch.Stop();
            _logger.LogInformation(
                "Statement preview final render completed in {ElapsedMs} ms; total render loop took {TotalElapsedMs} ms",
                finalRenderStopwatch.ElapsedMilliseconds,
                renderLoopStopwatch.ElapsedMilliseconds);

            string previewToken = Guid.NewGuid().ToString("N");
            string requestSignature = BuildPreviewSignature(request, startDate, endDate);

            _memoryCache.Set(
                BuildPreviewCacheKey(previewToken),
                new PreviewCacheEntry
                {
                    PdfBytes = previewPdf,
                    NumberOfPages = numberOfPages,
                    RequestSignature = requestSignature
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                }
            );

            return Ok(new PreviewResponseDTO
            {
                PreviewToken = previewToken,
                NumberOfPages = numberOfPages,
                TotalCharge = chargePreview.TotalCharge,
                AccountToCharge = chargePreview.AccountCharged,
                ChargeMessage = chargePreview.Message,
                AccountName = statement.AccountName,
                AccountNumber = statement.AccountNumber,
                Branch = statement.Branch,
                AccountType = statement.AccountType,
                BookBalance = statement.BookBalance,
                ClearBalance = statement.ClearBalance,
                TotalDebitValue = statement.TotalDebitValue,
                TotalCreditValue = statement.TotalCreditValue,
                TotalDebitCount = statement.TotalDebitCount,
                TotalCreditCount = statement.TotalCreditCount
            });
        }

        // POST /api/statement/generate
        // Processes charge and returns PDF
        [HttpPost("generate")]
        public async Task<IActionResult> Generate(
            [FromBody] StatementRequestDTO request)
        {
            var totalStopwatch = Stopwatch.StartNew();

            // Step 1 — Validate request
            var validation = ValidateRequest(request);
            if (validation != null) return validation;

            // Step 2 — Parse dates
            var parseResult = TryParseDateRange(request.StartDate, request.EndDate);
            if (!parseResult.Success)
                return BadRequest(new { message = parseResult.Message });

            DateTime startDate = parseResult.StartDate;
            DateTime endDate = parseResult.EndDate;

            // Step 3 — Fetch statement from bank API
            var bankFetchStopwatch = Stopwatch.StartNew();
            var statementResult = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate, request.Channel
            );
            bankFetchStopwatch.Stop();
            _logger.LogInformation(
                "Statement generation bank fetch completed in {ElapsedMs} ms for account {AccountNumber}",
                bankFetchStopwatch.ElapsedMilliseconds,
                request.AccountNumber);

            if (!statementResult.Success)
            {
                if (statementResult.StatementNotFound)
                    return NotFound(new
                    {
                        message = statementResult.Message,
                        code = statementResult.ErrorCode,
                        selectedChannel = statementResult.SelectedChannel,
                        suggestedChannel = statementResult.SuggestedChannel
                    });

                return StatusCode(503, new { message = statementResult.Message });
            }

            var statement = statementResult.Statement!;
            statement.Channel = request.Channel;
            statement.StartDate = DateOnly.FromDateTime(startDate);
            statement.EndDate = DateOnly.FromDateTime(endDate);

            string previewToken = request.PreviewToken?.Trim() ?? string.Empty;
            byte[]? cachedPreviewPdf = null;
            int numberOfPages;

            if (!string.IsNullOrEmpty(previewToken))
            {
                string cacheKey = BuildPreviewCacheKey(previewToken);

                if (!_memoryCache.TryGetValue(cacheKey, out PreviewCacheEntry? cachedPreview) ||
                    cachedPreview == null)
                {
                    return BadRequest(new
                    {
                        message = "Preview has expired. Please preview again before printing."
                    });
                }

                string incomingSignature = BuildPreviewSignature(request, startDate, endDate);
                if (!string.Equals(incomingSignature, cachedPreview.RequestSignature, StringComparison.Ordinal))
                {
                    return BadRequest(new
                    {
                        message = "Statement details changed after preview. Please preview again before printing."
                    });
                }

                numberOfPages = cachedPreview.NumberOfPages;
                cachedPreviewPdf = cachedPreview.PdfBytes;
            }
            else
            {
                // Fallback path for clients that do not send preview token.
                numberOfPages = _pdfService.CalculateNumberOfPages(statement);
            }

            // Step 5 — Process charge (actually debits account)
            var chargingResult = await _chargingService.ProcessCharge(
                request, numberOfPages
            );

            // Step 6 — Stop if charge failed
            if (chargingResult.Status == ChargeStatus.Failed)
                return BadRequest(new { message = chargingResult.Message });

            // Step 7 — Generate PDF
            var pdfStopwatch = Stopwatch.StartNew();
            byte[] pdf = cachedPreviewPdf ?? _pdfService.GenerateStatement(statement, chargingResult);
            pdfStopwatch.Stop();
            _logger.LogInformation(
                "Statement generation PDF step completed in {ElapsedMs} ms for account {AccountNumber}",
                pdfStopwatch.ElapsedMilliseconds,
                request.AccountNumber);

            string staffUsername = User.Identity?.Name ?? request.StaffUsername;
            string staffFullName =
                User.FindFirst(ClaimTypes.GivenName)?.Value ?? staffUsername;
            string staffId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? staffUsername;

            try
            {
                await _auditService.LogStatement(
                    staffUsername,
                    staffFullName,
                    request.AccountNumber,
                    statement.AccountName,
                    startDate,
                    endDate,
                    request.Channel,
                    staffId,
                    chargingResult
                );
            }
            catch
            {
                // Keep statement generation successful even if audit insert fails.
            }

            if (!string.IsNullOrEmpty(previewToken))
            {
                _memoryCache.Remove(BuildPreviewCacheKey(previewToken));
            }

            totalStopwatch.Stop();
            _logger.LogInformation(
                "Statement generation finished in {ElapsedMs} ms for account {AccountNumber}",
                totalStopwatch.ElapsedMilliseconds,
                request.AccountNumber);

            // Step 8 — Return PDF as downloadable file
            return File(pdf, "application/pdf",
                $"Statement_{request.AccountNumber}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private static string BuildPreviewCacheKey(string previewToken) =>
            $"{PreviewCachePrefix}{previewToken}";

        private static string BuildPreviewSignature(
            StatementRequestDTO request,
            DateTime startDate,
            DateTime endDate)
        {
            string alt = request.AltAccountNumber?.Trim() ?? string.Empty;
            return string.Join("|",
                request.AccountNumber.Trim(),
                startDate.ToString("yyyy-MM-dd"),
                endDate.ToString("yyyy-MM-dd"),
                request.Channel.Trim().ToUpperInvariant(),
                request.WaiveCharge,
                request.ChargeAltAccount,
                alt
            );
        }

        private sealed class PreviewCacheEntry
        {
            public byte[] PdfBytes { get; init; } = Array.Empty<byte>();
            public int NumberOfPages { get; init; }
            public string RequestSignature { get; init; } = string.Empty;
        }

        // Validates the incoming request
        private IActionResult? ValidateRequest(StatementRequestDTO request)
        {
            if (string.IsNullOrEmpty(request.AccountNumber))
                return BadRequest(new { message = "Account number is required" });

            if (string.IsNullOrEmpty(request.StartDate))
                return BadRequest(new { message = "Start date is required" });

            if (string.IsNullOrEmpty(request.EndDate))
                return BadRequest(new { message = "End date is required" });

            if (string.IsNullOrEmpty(request.Channel))
                return BadRequest(new { message = "Channel is required" });

            if (request.Channel.ToUpper() != "VISA" &&
                request.Channel.ToUpper() != "ESB")
                return BadRequest(new { message = "Channel must be VISA or ESB" });

            if (request.ChargeAltAccount &&
                string.IsNullOrEmpty(request.AltAccountNumber))
                return BadRequest(new
                {
                    message = "Alt account number is required when charging alt account"
                });

            return null;
        }

        private static (bool Success, DateTime StartDate, DateTime EndDate, string Message)
            TryParseDateRange(string startDateInput, string endDateInput)
        {
            string[] acceptedFormats =
            {
                "yyyy-MM-dd",
                "dd-MM-yyyy",
                "yyyyMMdd",
                "dd/MM/yyyy",
                "MM/dd/yyyy"
            };

            bool startIsValid = DateTime.TryParseExact(
                startDateInput,
                acceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var startDate
            );

            bool endIsValid = DateTime.TryParseExact(
                endDateInput,
                acceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var endDate
            );

            if (!startIsValid || !endIsValid)
            {
                return (
                    false,
                    default,
                    default,
                    "Invalid date format. Use yyyy-MM-dd or dd-MM-yyyy."
                );
            }

            if (startDate > endDate)
            {
                return (
                    false,
                    default,
                    default,
                    "Start date cannot be later than end date."
                );
            }

            return (true, startDate, endDate, string.Empty);
        }
    }
}