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

            //Basic authentication setup for all requests to the bank API
            string username = _config["BankApi:Username"]!;
            string password = _config["BankApi:Password"]!;
            string credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}: {password}")
            );

            _httpClient.DefaultRequestHeaders.Add(
                "credential", credentials
            );

            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            _httpClient.DefaultRequestHeaders.Add(
                "disablePagination","true"
            );
        }

        public async Task<AccountLookupDTO?> GetAccountDetails(string accountNumber)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;
                string companyId = _config["BankApi:CompanyId"]!;


                _httpClient.DefaultRequestHeaders.Remove("companyId");
                _httpClient.DefaultRequestHeaders.Add("companyId", companyId);

                var response = await _httpClient.GetAsync(
                    $"{baseUrl}/party/umbGetAccountInfo/?accountNo={accountNumber}"
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                    //Parsing nested response structure

                    var result = await response.Content.ReadFromJsonAsync<BankApiResponse<List<AccountInfo>>>();

                    if (result?.Body == null || !result.Body.Any())
                        return null;

                var account = result.Body.First();

                return new AccountLookupDTO
                {
                    AccountNumber= account.AccountNumber,
                    AccountName = account.Name
                };
            }
            catch
            {
                return null;
            }
        }

       // Get statement using the getAccountStatements endpoint
    public async Task<Statement?> GetStatement(
        string accountNumber,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            string baseUrl = _config["BankApi:BaseUrl"]!;

            // Note the date format is YYYYMMDD not YYYY-MM-DD
            string url = $"{baseUrl}/party/account/getAccountStatements.2.1.0" +
                         $"?accountNumber={accountNumber}" +
                         $"&startDate={startDate:yyyyMMdd}" +
                         $"&endDate={endDate:yyyyMMdd}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content
                .ReadFromJsonAsync<BankApiResponse<Statement>>();

            if (result?.Body == null)
                return null;

            result.Body.AccountNumber = string.IsNullOrWhiteSpace(result.Body.AccountNumber)
                ? accountNumber
                : result.Body.AccountNumber;
            result.Body.StartDate = startDate;
            result.Body.EndDate = endDate;

            return result.Body;
        }
        catch
        {
            return null;
        }
    }
}
}