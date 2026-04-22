using BankStatementAPI.DTOs;
using BankStatementAPI.Models;

namespace BankStatementAPI.Services
{
    public class ChargingService
    {
        private readonly BankApiService _bankApiService;
        private readonly IConfiguration _config;

        public ChargingService(BankApiService bankApiService, IConfiguration config)
        {
            _bankApiService = bankApiService;
            _config = config;
        }

        // Calculates the charge without actually debiting — used for preview
        public ChargingResult PreviewCharge(
            StatementRequestDTO request,
            int numberOfPages)
        {
            decimal chargePerPage = _config.GetValue<decimal>(
                "Charging:VisaChargePerPage"
            );

            // Rule 1 — ESB is always free
            if (request.Channel.ToUpper() == "ESB")
            {
                return new ChargingResult
                {
                    TotalCharge = 0,
                    AccountCharged = string.Empty,
                    Status = ChargeStatus.Free,
                    NumberOfPages = numberOfPages,
                    Message = "No charge applicable for ESB channel"
                };
            }

            // Rule 2 — VISA but charge is waived
            if (request.WaiveCharge)
            {
                return new ChargingResult
                {
                    TotalCharge = 0,
                    AccountCharged = string.Empty,
                    Status = ChargeStatus.Waived,
                    NumberOfPages = numberOfPages,
                    Message = $"Charge has been waived."
                };
            }

            // Rule 3 — VISA not waived, determine account to charge
            string accountToCharge = request.ChargeAltAccount
                ? request.AltAccountNumber!
                : request.AccountNumber;

            decimal totalCharge = chargePerPage * numberOfPages;

            return new ChargingResult
            {
                TotalCharge = totalCharge,
                AccountCharged = accountToCharge,
                Status = ChargeStatus.Success,
                NumberOfPages = numberOfPages,
                Message = $"GHS {totalCharge} will be charged to account {accountToCharge}"
            };
        }

        // Actually processes the charge — used when generating the PDF
        public async Task<ChargingResult> ProcessCharge(
            StatementRequestDTO request,
            int numberOfPages)
        {
            // Get the preview first
            var preview = PreviewCharge(request, numberOfPages);

            // If free or waived no debit needed
            if (preview.Status == ChargeStatus.Free ||
                preview.Status == ChargeStatus.Waived)
            {
                return preview;
            }

          /*  // Attempt to debit the account
            bool debitSuccessful = await _bankApiService
                .DebitAccount(preview.AccountCharged!, preview.TotalCharge);

            if (!debitSuccessful)
            {
                return new ChargingResult
                {
                    TotalCharge = preview.TotalCharge,
                    AccountCharged = preview.AccountCharged,
                    Status = ChargeStatus.Failed,
                    NumberOfPages = numberOfPages,
                    Message = $"Insufficient funds on account {preview.AccountCharged}"
                };
            }
*/
            return new ChargingResult
            {
                TotalCharge = preview.TotalCharge,
                AccountCharged = preview.AccountCharged,
                Status = ChargeStatus.Success,
                NumberOfPages = numberOfPages,
                Message = $"GHS {preview.TotalCharge} charged to account {preview.AccountCharged}"
            };
        }
    }
}