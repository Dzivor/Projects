using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using System.Text.Json;

namespace BankStatementAPI.Services
{
    public class BankApiService
    {
        private const string AccountNotFoundInChannelCode = "ACCOUNT_NOT_FOUND_IN_CHANNEL";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BankApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        // ─────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────

        private void AddBankApiHeaders(HttpRequestMessage request, string? channel)
        {
            string signOn = _config["BankApi:SignOn"]!;
            string companyId = ResolveCompanyId(channel);

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
        // Now also returns balance for charge check
        // ─────────────────────────────────────────

        public async Task<AccountLookupResultDTO> GetAccountDetails(
            string accountNumber,
            string? channel)
        {
            string selectedChannel = NormalizeChannel(channel);

            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string url = $"{baseUrl}/party/umbGetAcctInfo/" +
                             $"?accountNo={accountNumber}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddBankApiHeaders(request, channel);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new AccountLookupResultDTO
                    {
                        Success = false,
                        AccountNotFound = false,
                        Message = "Unable to verify account details at this time. " +
                                  "Please try again later."
                    };

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

                // Parse balance from the bank API response
                // accountBalance comes back as a string e.g "219641.11"
                decimal.TryParse(
                    account.AccountBalance,
                    out decimal accountBalance
                );

                return new AccountLookupResultDTO
                {
                    Success = true,
                    Account = new AccountLookupDTO
                    {
                        AccountNumber = account.AccountNumber,
                        AccountName = account.Name,
                        AccountBalance = accountBalance  // ← new
                    }
                };
            }
            catch
            {
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
        // No changes — kept exactly as you had it
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
                AddBankApiHeaders(request, channel);

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

                // Empty body — no transactions but call was successful
                // Still return the statement so PDF can be generated
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

        // ─────────────────────────────────────────
        // STATEMENT MAPPING
        // No changes — kept exactly as you had it
        // ─────────────────────────────────────────

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
                OpeningBalance = decimal.TryParse(
                    data.OpeningBalance, out var ob) ? ob : 0,
                BookBalance = decimal.TryParse(
                    data.TotalAmount, out var ba) ? ba : 0,
                ClearBalance = decimal.TryParse(
                    data.ClearedBalance, out var cb) ? cb : 0,
                TotalDebitValue = decimal.TryParse(
                    data.TotalDebit, out var td) ? td : 0,
                TotalCreditValue = decimal.TryParse(
                    data.TotalCredit, out var tc) ? tc : 0,
                TotalDebitCount = apiResponse.Body
                    .Count(t => !string.IsNullOrEmpty(t.DebitAmount)
                        && t.DebitAmount != "0"),
                TotalCreditCount = apiResponse.Body
                    .Count(t => !string.IsNullOrEmpty(t.CreditAmount)
                        && t.CreditAmount != "0"),
                Transactions = apiResponse.Body.Select(t =>
                {
                    var transactionType = string.IsNullOrWhiteSpace(t.TransactionType)
                        ? "-" : t.TransactionType;

                    var description = string.Concat(t.Descriptions
                        .Select(d => NormalizeDescriptionForNarrative(d.Description))
                        .Where(d => !string.IsNullOrWhiteSpace(d)))
                        .Trim();

                    return new Transaction
                    {
                        BookingDate = DateTime.TryParse(
                            t.BookingDate, out var bd) ? bd : DateTime.MinValue,
                        Narrative = $"{transactionType}: " +
                                    $"{(string.IsNullOrWhiteSpace(description) ? "-" : description)}",
                        ValueDate = DateTime.TryParse(
                            t.ValueDate, out var vd) ? vd : DateTime.MinValue,
                        Debit = decimal.TryParse(
                            t.DebitAmount, out var da) ? da : 0,
                        Credit = decimal.TryParse(
                            t.CreditAmount, out var ca) ? ca : 0,
                        Balance = decimal.TryParse(
                            t.ClosingBalance, out var clb) ? clb : 0
                    };
                }).ToList()
            };
        }

        // ─────────────────────────────────────────
        // DEBIT ACCOUNT
        // Updated to return DebitResult instead of bool
        // so we can capture the transaction reference
        // ─────────────────────────────────────────

        public async Task<DebitResult> DebitAccount(
            string accountNumber,
            decimal amount,
            string channel)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string url = $"{baseUrl}/party/payments/createGenericTransfer";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                AddBankApiHeaders(request, channel);

                var body = new
                {
                    header = new { },
                    body = new
                    {
                        transactionType = "AC",
                        debitAccountId = accountNumber,
                        debitCurrency = "GHS",
                        debitAmount = amount,
                        // Credit account read from config — never hardcoded
                        creditAccountId = _config["BankApi:ChargeCollectionAccount"]
                    }
                };

                request.Content = JsonContent.Create(body);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    string shortMessage = ExtractBankErrorMessage(responseBody)
                        ?? $"Bank API returned {response.StatusCode}";
                    return new DebitResult
                    {
                        Success = false,
                        UserMessage = shortMessage,
                        ErrorMessage = string.IsNullOrWhiteSpace(responseBody)
                            ? $"Bank API returned {response.StatusCode}"
                            : $"Bank API returned {response.StatusCode}: {responseBody}"
                    };
                }

                var result = await response.Content
                    .ReadFromJsonAsync<BankApiTransferResponse>();

                if (result?.Header.Status != "success")
                    return new DebitResult
                    {
                        Success = false,
                        ErrorMessage = "Transaction was not confirmed by bank API"
                    };

                return new DebitResult
                {
                    Success = true,
                    // Bank transaction reference e.g FT22265SJC32
                    // Stored in audit log for traceability
                    TransactionReference = result.Header.Id
                };
            }
            catch (Exception ex)
            {
                return new DebitResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    UserMessage = ex.Message
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

    // ─────────────────────────────────────────
    // DEBIT RESULT
    // Richer return type than bool —
    // captures success, reference and error
    // ─────────────────────────────────────────

    public class DebitResult
    {
        public bool Success { get; set; }
        public string? TransactionReference { get; set; }
        public string? ErrorMessage { get; set; }
        public string? UserMessage { get; set; }
    }
}