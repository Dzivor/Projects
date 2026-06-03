namespace BankStatementAPI.DTOs
{
    public class DashboardStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int DisabledUsers { get; set; }
        public int StatementsToday { get; set; }
        public int StatementsTodayVisa { get; set; }
        public int StatementsTodayEsb { get; set; }
        public decimal ChargesToday { get; set; }
        public int StatementsThisMonth { get; set; }
        public decimal ChargesThisMonth { get; set; }
        public List<StaffActivityDTO> MostActiveStaff { get; set; } = new();
    }

    public class StaffActivityDTO
    {
        public string FullName { get; set; } = "";
        public string Username { get; set; } = "";
        public int StatementCount { get; set; }
        public string PrimaryChannel { get; set; } = "";
        public decimal TotalCharged { get; set; }
    }
}