using System.Text;
using BankStatementAPI.DTOs;
using BankStatementAPI.Models;

namespace BankStatementAPI.Services
{
    public class BankApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BankApiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private void AddBankApiHeaders(HttpRequestMessage request)
        {
            string signOn = _config["BankApi:SignOn"]!;
            string companyId = _config["BankApi:CompanyId"]!;

            request.Headers.Add("credentials", signOn);
            request.Headers.Add("companyId", companyId);
            request.Headers.Add("Accept", "application/json");
        }

        public async Task<AccountLookupResultDTO> GetAccountDetails(string accountNumber)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string url = $"{baseUrl}/party/umbGetAcctInfo/?accountNo={accountNumber}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                AddBankApiHeaders(request);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new AccountLookupResultDTO
                    {
                        Success = false,
                        AccountNotFound = false,
                        Message = "Unable to verify account details at this time. Please try again later."
                    };

                var result = await response.Content
                    .ReadFromJsonAsync<BankApiAccountResponse>();

                var account = result?.Body?.FirstOrDefault();

                if (account == null || account.SuccessIndicator != "Success")
                    return new AccountLookupResultDTO
                    {
                        Success = false,
                        AccountNotFound = true,
                        Message = "Account does not exist"
                    };

                return new AccountLookupResultDTO
                {
                    Success = true,
                    Account = new AccountLookupDTO
                    {
                        AccountNumber = account.AccountNumber,
                        AccountName = account.Name
                    }
                };
            }
            catch
            {
                return new AccountLookupResultDTO
                {
                    Success = false,
                    AccountNotFound = false,
                    Message = "Unable to verify account details at this time. Please try again later."
                };
            }
        }

        public async Task<Statement?> GetStatement(
            string accountNumber,
            DateTime startDate,
            DateTime endDate)
        {
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
                AddBankApiHeaders(request);

                request.Headers.Add("disablePagination", "true");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return null;

                var apiResponse = await response.Content
                    .ReadFromJsonAsync<BankApiStatementResponse>();

                if (apiResponse == null || apiResponse.Header.Status != "success")
                    return null;

                return MapToStatement(apiResponse);
            }
            catch
            {
                return null;
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
                Transactions = apiResponse.Body.Select(t => new Transaction
                {
                    BookingDate = DateTime.TryParse(t.BookingDate, out var bd) ? bd : DateTime.MinValue,
                    Narrative = string.Join(" ", t.Descriptions.Select(d => d.Description)),
                    ValueDate = DateTime.TryParse(t.ValueDate, out var vd) ? vd : DateTime.MinValue,
                    Debit = decimal.TryParse(t.DebitAmount, out var da) ? da : 0,
                    Credit = decimal.TryParse(t.CreditAmount, out var ca) ? ca : 0,
                    Balance = decimal.TryParse(t.ClosingBalance, out var clb) ? clb : 0
                }).ToList()
            };
        }

        public async Task<bool> DebitAccount(string accountNumber, decimal amount)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string url = $"{baseUrl}/party/payments/createGenericTransfer";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                AddBankApiHeaders(request);

                var body = new
                {
                    header = new { },
                    body = new
                    {
                        transactionType = "AC",
                        debitAccountId = accountNumber,
                        debitCurrency = "GHS",
                        debitAmount = amount,
                        creditAccountId = _config["BankApi:ChargeCollectionAccount"]
                    }
                };

                request.Content = JsonContent.Create(body);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content
                    .ReadFromJsonAsync<BankApiTransferResponse>();

                return result?.Header.Status == "success";
            }
            catch
            {
                return false;
            }
        }
    }
}