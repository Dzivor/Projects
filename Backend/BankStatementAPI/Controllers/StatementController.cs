using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using BankStatementAPI.Services;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace BankStatementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatementController : ControllerBase
    {
        private readonly BankApiService _bankApiService;
        private readonly ChargingService _chargingService;
        private readonly PdfService _pdfService;

        public StatementController(
            BankApiService bankApiService,
            ChargingService chargingService,
            PdfService pdfService)
        {
            _bankApiService = bankApiService;
            _chargingService = chargingService;
            _pdfService = pdfService;
        }

        // POST /api/statement/preview
        // Shows charge info before actually generating the PDF
        [HttpPost("preview")]
        public async Task<IActionResult> Preview(
            [FromBody] StatementRequestDTO request)
        {
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
            var statementResult = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate
            );

            if (!statementResult.Success)
            {
                if (statementResult.StatementNotFound)
                    return NotFound(new { message = statementResult.Message });

                return StatusCode(503, new { message = statementResult.Message });
            }

            var statement = statementResult.Statement!;

            // Step 4 — Calculate number of pages
            int numberOfPages = _pdfService.CalculateNumberOfPages(statement);

            // Step 5 — Calculate charge (no debit yet)
            var chargePreview = _chargingService.PreviewCharge(
                request, numberOfPages
            );

            return Ok(new PreviewResponseDTO
            {
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
            var statementResult = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate
            );

            if (!statementResult.Success)
            {
                if (statementResult.StatementNotFound)
                    return NotFound(new { message = statementResult.Message });

                return StatusCode(503, new { message = statementResult.Message });
            }

            var statement = statementResult.Statement!;

            // Step 4 — Calculate pages
            int numberOfPages = _pdfService.CalculateNumberOfPages(statement);

            // Step 5 — Process charge (actually debits account)
            var chargingResult = await _chargingService.ProcessCharge(
                request, numberOfPages
            );

            // Step 6 — Stop if charge failed
            if (chargingResult.Status == ChargeStatus.Failed)
                return BadRequest(new { message = chargingResult.Message });

            // Step 7 — Generate PDF
            statement.Channel = request.Channel;
            statement.StartDate = startDate;
            statement.EndDate = endDate;
            byte[] pdf = _pdfService.GenerateStatement(statement, chargingResult);

            // Step 8 — Return PDF as downloadable file
            return File(pdf, "application/pdf",
                $"Statement_{request.AccountNumber}_{DateTime.Now:yyyyMMdd}.pdf");
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