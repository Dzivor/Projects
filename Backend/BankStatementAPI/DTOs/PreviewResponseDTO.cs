namespace BankStatementAPI.DTOs
{
    // What your API sends back for the preview before generating PDF
    public class PreviewResponseDTO
    {
        public int NumberOfPages { get; set; }
        public decimal TotalCharge { get; set; }
        public string? AccountToCharge { get; set; }
        public string ChargeMessage { get; set; } = "";
        public string AccountName { get; set; } = "";
    }
}