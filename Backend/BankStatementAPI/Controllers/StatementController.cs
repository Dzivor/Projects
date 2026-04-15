using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using BankStatementAPI.Services;
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
            DateTime startDate = DateTime.Parse(request.StartDate);
            DateTime endDate = DateTime.Parse(request.EndDate);

            // Step 3 — Fetch statement from bank API
            var statement = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate
            );

            if (statement == null)
                return NotFound(new { message = "No statement found" });

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
                AccountName = statement.AccountName
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
            DateTime startDate = DateTime.Parse(request.StartDate);
            DateTime endDate = DateTime.Parse(request.EndDate);

            // Step 3 — Fetch statement from bank API
            var statement = await _bankApiService.GetStatement(
                request.AccountNumber, startDate, endDate
            );

            if (statement == null)
                return NotFound(new { message = "No statement found" });

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
    }
}