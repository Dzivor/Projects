namespace BankStatementAPI.Models
{
    // Represents the outcome after charging logic runs
    public class ChargingResult
    {
        public decimal TotalCharge { get; set; }
        public string? AccountCharged { get; set; }
        public ChargeStatus Status { get; set; }
        public string Message { get; set; } = "";
        public int NumberOfPages { get; set; }
    }
}