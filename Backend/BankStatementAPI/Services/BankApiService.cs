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

        public async Task<AccountLookupDTO?> GetAccountDetails(string accountNumber)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;

                var response = await _httpClient.GetAsync(
                    $"{baseUrl}/accounts/{accountNumber}"
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content
                    .ReadFromJsonAsync<AccountLookupDTO>();
            }
            catch
            {
                return null;
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

                var response = await _httpClient.GetAsync(
                    $"{baseUrl}/statements" +
                    $"?accountNumber={accountNumber}" +
                    $"&startDate={startDate:yyyy-MM-dd}" +
                    $"&endDate={endDate:yyyy-MM-dd}"
                );

                if (!response.IsSuccessStatusCode)
                    return null;

                // ReadFromJsonAsync maps the JSON response
                // to our updated Statement model automatically
                // Field names in the JSON must match property names
                // in the Statement class
                var statement = await response.Content
                    .ReadFromJsonAsync<Statement>();

                return statement;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DebitAccount(
            string accountNumber,
            decimal amount)
        {
            try
            {
                string baseUrl = _config["BankApi:BaseUrl"]!;

                var response = await _httpClient.PostAsJsonAsync(
                    $"{baseUrl}/accounts/debit",
                    new { accountNumber, amount }
                );

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}