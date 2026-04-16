namespace BankStatementAPI.Models
{
    public class Transaction
    {
        public DateTime BookingDate{get; set;}
        public DateTime ValueDate{get; set;}
        public string Narrative{get;set;}="";
        public DateTime Date{get; set;}
        
        public decimal Debit{get; set;}
        public decimal Credit{get; set;}

        public decimal Balance{get; set;}
        
    }
}