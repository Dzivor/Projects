using BankStatementAPI.DTOs;
using BankStatementAPI.Models;

namespace BankStatementAPI.Services
{
    public class ChargingService
    {
        private readonly BankApiService _bankApiService;
        private readonly SettingsService _settingsService;

        public ChargingService(BankApiService bankApiService, SettingsService settingsService)
        {
            _bankApiService = bankApiService;
            _settingsService = settingsService;
        }

        // ─────────────────────────────────────────
        // PREVIEW CHARGE
        // Calculates charge without debiting
        // Used to show user what they will be charged
        // ─────────────────────────────────────────

        public async Task<ChargingResult> PreviewCharge(
            StatementRequestDTO request,
            int numberOfPages)
        {
            decimal chargePerPage = await _settingsService
                .GetDecimalSetting("VisaChargePerPage", 12.00m);

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
                    Message = "Charge has been waived"
                };
            }

            // Rule 3 — VISA not waived
            // Determine which account to charge
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
                Message = $"GHS {totalCharge} will be charged to " +
                          $"account {accountToCharge}"
            };
        }

        // ─────────────────────────────────────────
        // PROCESS CHARGE
        // 1. Check balance of account to be charged
        // 2. If sufficient — debit the account
        // 3. If insufficient — stop, return error
        // ─────────────────────────────────────────

        public async Task<ChargingResult> ProcessCharge(
            StatementRequestDTO request,
            int numberOfPages)
        {
            // Step 1 — Calculate what the charge will be
            var preview = await PreviewCharge(request, numberOfPages);

            // Step 2 — If free or waived no further action needed
            if (preview.Status == ChargeStatus.Free ||
                preview.Status == ChargeStatus.Waived)
            {
                return preview;
            }

            // Step 3 — Check balance of the account to be charged
            // This uses the same account lookup endpoint
            // which now returns the balance
            var accountToCharge = preview.AccountCharged!;

            var accountDetails = await _bankApiService
                .GetAccountDetails(accountToCharge, request.Channel);

            // Step 4 — Account lookup failed
            if (!accountDetails.Success || accountDetails.Account == null)
            {
                string message = request.ChargeAltAccount
                    ? "Invalid charge account. Please provide a vaild UMB account."
                    : accountDetails.Message ?? $"Could not verify account {accountToCharge}";

                return new ChargingResult
                {
                    TotalCharge = preview.TotalCharge,
                    AccountCharged = accountToCharge,
                    Status = ChargeStatus.Failed,
                    NumberOfPages = numberOfPages,
                    Message = message
                };
            }

            // Step 5 — Check if balance is sufficient
            decimal availableBalance = accountDetails.Account.AccountBalance;
            decimal requiredCharge = preview.TotalCharge;

            if (availableBalance < requiredCharge)
            {
                // Balance too low — do not attempt debit
                // Tell the user exactly what is needed vs what is available
                return new ChargingResult
                {
                    TotalCharge = requiredCharge,
                    AccountCharged = accountToCharge,
                    Status = ChargeStatus.Failed,
                    NumberOfPages = numberOfPages,
                    Message = $"Insufficient funds on account {accountToCharge}. " +
                              $"Available balance: GHS {availableBalance:N2}. " +
                              $"Required: GHS {requiredCharge:N2}."
                };
            }

            // Step 6 — Balance is sufficient — attempt debit
            var debitResult = await _bankApiService.DebitAccount(
                accountToCharge,
                requiredCharge,
                request.Channel
            );

            // Step 7 — Debit failed
            if (!debitResult.Success)
            {
                return new ChargingResult
                {
                    TotalCharge = requiredCharge,
                    AccountCharged = accountToCharge,
                    Status = ChargeStatus.Failed,
                    NumberOfPages = numberOfPages,
                    Message = debitResult.UserMessage
                        ?? $"Transaction failed on account {accountToCharge}",
                    ErrorDetails = debitResult.ErrorMessage
                };
            }

            // Step 8 — Everything succeeded
            return new ChargingResult
            {
                TotalCharge = requiredCharge,
                AccountCharged = accountToCharge,
                Status = ChargeStatus.Success,
                NumberOfPages = numberOfPages,
                BankTransactionReference = debitResult.TransactionReference,
                Message = $"GHS {requiredCharge:N2} successfully charged to " +
                          $"account {accountToCharge}. " +
                          $"Reference: {debitResult.TransactionReference}"
            };
        }
    }
}