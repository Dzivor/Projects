using BankStatementAPI.Data;
using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace BankStatementAPI.Services
{
    public class BankApiService
    {
        private const string AccountNotFoundInChannelCode = "ACCOUNT_NOT_FOUND_IN_CHANNEL";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly SettingsService _settingsService;
        private readonly AppDbContext _context;

        public BankApiService(
            HttpClient httpClient,
            IConfiguration config,
            SettingsService settingsService,
            AppDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _settingsService = settingsService;
            _context = context;
        }

        // ─────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────

        private async Task AddBankApiHeadersAsync(
            HttpRequestMessage request,
            string? channel)
        {
            string companyId = ResolveCompanyId(channel);

            // credentials header uses SignOn, which is now editable in DB (AppSettings).
            // Fallback to appsettings.json if the DB key is missing.
            string signOn = await _settingsService.GetSettingValue(
                "BankApi:SignOn",
                _config["BankApi:SignOn"] ?? "");

            if (string.IsNullOrWhiteSpace(signOn))
            {
                throw new InvalidOperationException(
                    "Bank API SignOn (BankApi:SignOn) is not configured.");
            }


            request.Headers.Add("credentials", signOn);
            request.Headers.Add("companyId", companyId);
            request.Headers.Add("Accept", "application/json");
        }

        private string ResolveCompanyId(string? channel)
        {
            string normalized = NormalizeChannel(channel);

            string? mapped = _config[$"BankApi:CompanyIds:{normalized}"];
            if (!string.IsNullOrWhiteSpace(mapped))
                return mapped;

            throw new InvalidOperationException(
                $"No companyId configured for channel '{normalized}'.");
        }

        private static string NormalizeChannel(string? channel)
        {
            return string.IsNullOrWhiteSpace(channel)
                ? "VISA"
                : channel.Trim().ToUpperInvariant();
        }

        private static string GetSuggestedChannel(string selectedChannel)
        {
            return selectedChannel == "ESB" ? "VISA" : "ESB";
        }

        private static string BuildChannelNotFoundMessage(string selectedChannel)
        {
            string suggestedChannel = GetSuggestedChannel(selectedChannel);
            return $"Account not found in {selectedChannel} records. " +
                   $"Please try the {suggestedChannel} channel.";
        }

        private static string NormalizeDescriptionForNarrative(string value)
        {
            string trimmed = value.TrimEnd();
            return trimmed.EndsWith('.')
                ? trimmed.TrimEnd('.') + " "
                : trimmed + " ";
        }

        // ─────────────────────────────────────────
        // ACCOUNT LOOKUP
        // ─────────────────────────────────────────

        public async Task<AccountLookupResultDTO> GetAccountDetails(
            string accountNumber,
            string? channel)
        {
            string selectedChannel = NormalizeChannel(channel);

            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string url = $"{baseUrl}/party/umbGetAcctInfo/?accountNo={accountNumber}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await AddBankApiHeadersAsync(request, channel);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = string.Empty;
                    try
                    {
                        responseBody = await response.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        // ignore response body read issues
                    }

                    // Do not log secrets/passwords. Log non-sensitive request context.
                    Serilog.Log.Warning(
                        "Account lookup failed — UpstreamStatus: {UpstreamStatus}, Url: {Url}, AccountNumber: {AccountNumber}, Channel: {Channel}, ResponseBody: {ResponseBody}",
                        (int)response.StatusCode,
                        url,
                        accountNumber,
                        selectedChannel,
                        string.IsNullOrWhiteSpace(responseBody) ? "<empty>" : responseBody);

                    return new AccountLookupResultDTO
                    {
                        Success = false,
                        AccountNotFound = false,
                        Message = "Unable to verify account details at this time. " +
                                  "Please try again later."
                    };
                }

                var result = await response.Content
                    .ReadFromJsonAsync<BankApiAccountResponse>();

                var account = result?.Body?.FirstOrDefault();

                if (account == null || account.SuccessIndicator != "Success")
                {
                    return new AccountLookupResultDTO
                    {
                        Success = false,
                        AccountNotFound = true,
                        Message = BuildChannelNotFoundMessage(selectedChannel),
                        ErrorCode = AccountNotFoundInChannelCode,
                        SelectedChannel = selectedChannel,
                        SuggestedChannel = GetSuggestedChannel(selectedChannel)
                    };
                }

                decimal.TryParse(account.AccountBalance, out decimal accountBalance);

                return new AccountLookupResultDTO
                {
                    Success = true,
                    Account = new AccountLookupDTO
                    {
                        AccountNumber = account.AccountNumber,
                        AccountName = account.Name,
                        AccountBalance = accountBalance
                    }
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(
                    ex,
                    "Account lookup exception — AccountNumber: {AccountNumber}, Channel: {Channel}",
                    accountNumber,
                    selectedChannel);

                return new AccountLookupResultDTO
                {
                    Success = false,
                    AccountNotFound = false,
                    Message = "Unable to verify account details at this time. " +
                              "Please try again later."
                };
            }
        }

        // ─────────────────────────────────────────
        // STATEMENT FETCH
        // ─────────────────────────────────────────

        public async Task<StatementLookupResultDTO> GetStatement(
            string accountNumber,
            DateTime startDate,
            DateTime endDate,
            string channel)
        {
            string selectedChannel = NormalizeChannel(channel);

            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;

                string start = startDate.ToString("yyyyMMdd");
                string end = endDate.ToString("yyyyMMdd");

                string url = $"{baseUrl}/party/account/getAccountStatements.2.1.0" +
                             $"?accountNumber={accountNumber}" +
                             $"&startDate={start}" +
                             $"&endDate={end}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                await AddBankApiHeadersAsync(request, channel);

                request.Headers.Add("disablePagination", "true");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new StatementLookupResultDTO
                    {
                        Success = false,
                        StatementNotFound = false,
                        Message = "Unable to fetch statement at this time. " +
                                  "Please try again later."
                    };

                var apiResponse = await response.Content
                    .ReadFromJsonAsync<BankApiStatementResponse>();

                if (apiResponse == null)
                    return new StatementLookupResultDTO
                    {
                        Success = false,
                        StatementNotFound = true,
                        Message = "Unable to fetch statement at this time. " +
                                  "Please try again later."
                    };

                if (apiResponse.Header.Status != "success")
                {
                    return new StatementLookupResultDTO
                    {
                        Success = false,
                        StatementNotFound = true,
                        Message = BuildChannelNotFoundMessage(selectedChannel),
                        ErrorCode = AccountNotFoundInChannelCode,
                        SelectedChannel = selectedChannel,
                        SuggestedChannel = GetSuggestedChannel(selectedChannel)
                    };
                }

                if (apiResponse.Body == null)
                    return new StatementLookupResultDTO
                    {
                        Success = false,
                        StatementNotFound = true,
                        Message = "Statement cannot be fetched at this time. " +
                                  "Please try again later."
                    };

                return new StatementLookupResultDTO
                {
                    Success = true,
                    StatementNotFound = false,
                    Statement = MapToStatement(apiResponse)
                };
            }
            catch
            {
                return new StatementLookupResultDTO
                {
                    Success = false,
                    StatementNotFound = false,
                    Message = "Unable to fetch statement at this time. " +
                              "Please try again later."
                };
            }
        }

        private Statement MapToStatement(BankApiStatementResponse apiResponse)
        {
            var data = apiResponse.Header.Data;

            return new Statement
            {
                AccountNumber = data.AccountNumber,
                AccountName = data.AccountTitle,
                Branch = data.Branch,
                AccountType = data.AccountType,
                ResidentialAddress = data.ResidentialAddress,
                StreetAddress = data.Street,
                PostalAddress = data.PostalAddress,
                BranchAddress = $"{data.Street}, {data.PostalAddress}",
                OpeningBalance = decimal.TryParse(data.OpeningBalance, out var ob) ? ob : 0,
                BookBalance = decimal.TryParse(data.TotalAmount, out var ba) ? ba : 0,
                ClearBalance = decimal.TryParse(data.ClearedBalance, out var cb) ? cb : 0,
                TotalDebitValue = decimal.TryParse(data.TotalDebit, out var td) ? td : 0,
                TotalCreditValue = decimal.TryParse(data.TotalCredit, out var tc) ? tc : 0,
                TotalDebitCount = apiResponse.Body
                    .Count(t => !string.IsNullOrEmpty(t.DebitAmount) && t.DebitAmount != "0"),
                TotalCreditCount = apiResponse.Body
                    .Count(t => !string.IsNullOrEmpty(t.CreditAmount) && t.CreditAmount != "0"),
                Transactions = apiResponse.Body.Select(t =>
                {
                    var transactionType = string.IsNullOrWhiteSpace(t.TransactionType)
                        ? "-" : t.TransactionType;

                    var description = string.Concat(t.Descriptions
                        .Select(d => NormalizeDescriptionForNarrative(d.Description))
                        .Where(d => !string.IsNullOrWhiteSpace(d))).Trim();

                    return new Transaction
                    {
                        BookingDate = DateTime.TryParse(t.BookingDate, out var bd) ? bd : DateTime.MinValue,
                        Narrative = $"{transactionType}: " +
                                    $"{(string.IsNullOrWhiteSpace(description) ? "-" : description)}",
                        ValueDate = DateTime.TryParse(t.ValueDate, out var vd) ? vd : DateTime.MinValue,
                        Debit = decimal.TryParse(t.DebitAmount, out var da) ? da : 0,
                        Credit = decimal.TryParse(t.CreditAmount, out var ca) ? ca : 0,
                        Balance = decimal.TryParse(t.ClosingBalance, out var clb) ? clb : 0
                    };
                }).ToList()
            };
        }

        // ─────────────────────────────────────────
        // DEBIT ACCOUNT (Charge)
        // ─────────────────────────────────────────

        public async Task<DebitResult> DebitAccount(
            string accountNumber,
            decimal amount,
            string channel,
            string statementAccountNumber,
            string staffUsername)
        {
            string creditAccount = await _settingsService.GetSettingValue(
                "ChargeCollectionAccount",
                _config["BankApi:ChargeCollectionAccount"] ?? "");

            string narrative = $"Statement charge for {statementAccountNumber}";

            // Create pending charge log before calling bank
            ChargeTransaction? chargeLog = null;

            try
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                chargeLog = new ChargeTransaction
                {
                    DebitAccountNumber = accountNumber,
                    CreditAccountNumber = creditAccount,
                    Amount = amount,
                    Channel = channel,
                    StatementAccountNumber = statementAccountNumber,
                    Status = ChargeTransactionStatus.Pending,
                    StaffUsername = staffUsername,
                    Narration = narrative,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ChargeTransactions.Add(chargeLog);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                Serilog.Log.Information(
                    "Charge initiated — Account: {AccountNumber}, Amount: GHS {Amount}, Statement: {StatementAccountNumber}, Staff: {StaffUsername}",
                    accountNumber,
                    amount,
                    statementAccountNumber,
                    staffUsername);

                // statementCharge contract
                string baseUrl = _config["BankApi:BaseUrl"]!;
                // Postman uses /party//account/statementCharge?validate_only=true
                // Keep path as upstream expects, but log payload diagnostics for 400 errors.
                string url = $"{baseUrl}/party//account/statementCharge";



                var request = new HttpRequestMessage(HttpMethod.Post, url);
                await AddBankApiHeadersAsync(request, channel);

                var body = new
                {
                    header = new { },
                    body = new
                    {
                        transactionType = "ACST",
                        debitAccountId = accountNumber,
                        debitCurrency = "GHS",
                        debitAmount = amount,
                        creditAccountId = creditAccount,
                        // Spec change: narrative renamed to paymentDetail
                        paymentDetails = new[]
                        {
                            new { paymentDetail = narrative }
                        }
                    }
                };

                // More diagnostics for upstream 400s (e.g. TOO MANY CHARACTERS)
                string narrativePreview = narrative.Length <= 80
                    ? narrative
                    : narrative.Substring(0, 80) + "...";

                Serilog.Log.Information(
                    "statementCharge request payload diagnostics — Url={Url}, debitAccountId={DebitAccountId}, creditAccountId={CreditAccountId}, debitAmount={DebitAmount}, narrativeLength={NarrativeLength}, narrativePreview={NarrativePreview}",
                    url,
                    accountNumber,
                    creditAccount,
                    amount,
                    narrative.Length,
                    narrativePreview);

                request.Content = JsonContent.Create(body);


                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    string shortMessage = ExtractBankErrorMessage(responseBody) ??
                                           $"Bank API returned {response.StatusCode}";

                    chargeLog.Status = ChargeTransactionStatus.Failed;
                    chargeLog.ErrorMessage = shortMessage;
                    chargeLog.CompletedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // Raw upstream details for debugging 400s (e.g. TOO MANY CHARACTERS)
                    string responseBodyPreview = responseBody.Length <= 500
                        ? responseBody
                        : responseBody.Substring(0, 500) + "...";

                    Serilog.Log.Warning(
                        "Charge failed (Upstream) — Url={Url}, UpstreamStatus={UpstreamStatus}, ErrorMessage={ErrorMessage}, ResponseBodyPreview={ResponseBodyPreview}",
                        url,
                        (int)response.StatusCode,
                        shortMessage,
                        responseBodyPreview);

                    return new DebitResult
                    {
                        Success = false,
                        ErrorMessage = shortMessage,
                        UserMessage = shortMessage,
                        ChargeTransactionId = chargeLog.Id
                    };
                }


                var result = await response.Content.ReadFromJsonAsync<BankApiTransferResponse>();

                if (result?.Header?.Status != "success")
                {
                    string errorMessage = "Transaction was not confirmed by bank API";

                    chargeLog.Status = ChargeTransactionStatus.Failed;
                    chargeLog.ErrorMessage = errorMessage;
                    chargeLog.CompletedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    Serilog.Log.Warning(
                        "Charge failed — Account: {AccountNumber}, Amount: GHS {Amount}, Error: {ErrorMessage}",
                        accountNumber,
                        amount,
                        errorMessage);

                    return new DebitResult
                    {
                        Success = false,
                        ErrorMessage = errorMessage,
                        UserMessage = errorMessage,
                        ChargeTransactionId = chargeLog.Id
                    };
                }

                chargeLog.Status = ChargeTransactionStatus.Success;
                chargeLog.BankTransactionReference = result.Header.Id;
                chargeLog.CompletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                Serilog.Log.Information(
                    "Charge succeeded — Ref: {TransactionReference}, Account: {AccountNumber}, Amount: GHS {Amount}",
                    result.Header.Id,
                    accountNumber,
                    amount);

                return new DebitResult
                {
                    Success = true,
                    TransactionReference = result.Header.Id,
                    ChargeTransactionId = chargeLog.Id
                };
            }
            catch (Exception ex)
            {
                if (chargeLog != null)
                {
                    chargeLog.Status = ChargeTransactionStatus.Failed;
                    chargeLog.ErrorMessage = ex.Message;
                    chargeLog.CompletedAt = DateTime.UtcNow;

                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        // ignore secondary failures
                    }
                }

                Serilog.Log.Error(
                    ex,
                    "Charge exception — Account: {AccountNumber}, Exception: {ExceptionMessage}",
                    accountNumber,
                    ex.Message);

                return new DebitResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    UserMessage = "Unable to process charge at this time.",
                    ChargeTransactionId = chargeLog?.Id
                };
            }
        }

        private static string? ExtractBankErrorMessage(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseBody);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("error", out JsonElement error) &&
                    error.TryGetProperty("errorDetails", out JsonElement details) &&
                    details.ValueKind == JsonValueKind.Array &&
                    details.GetArrayLength() > 0)
                {
                    JsonElement firstDetail = details[0];
                    if (firstDetail.TryGetProperty("message", out JsonElement message) &&
                        message.ValueKind == JsonValueKind.String)
                    {
                        return message.GetString();
                    }
                }
            }
            catch
            {
                // Ignore parse failures and fall back to the generic status code message.
            }

            return null;
        }
    }

    public class DebitResult
    {
        public bool Success { get; set; }
        public string? TransactionReference { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserMessage { get; set; }
        public int? ChargeTransactionId { get; set; }
    }
}

