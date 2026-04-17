namespace BankStatementAPI.Models
{
    //Generic wrapper for all the bank API responses
    public class BankApiResponse<T>
    {
        public BankApiHeader? Header {get; set;}
        public T? Body { get; set; }
    }

    public class BankApiHeader
    {
        public string? Status {get; set;}
        public int? TotalSize {get; set;}

        public int? PageSize {get; set;}
    }
    // Account info response body
    public class AccountInfo
    {
         public string AccountNumber { get; set; } = "";
        public string Name { get; set; } = "";
        public string AccountStatus { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string Currency { get; set; } = "";
        public string AccountBalance { get; set; } = "";
        public string SuccessIndicator { get; set; } = "";
    }
}