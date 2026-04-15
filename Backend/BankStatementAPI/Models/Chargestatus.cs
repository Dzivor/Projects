namespace BankStatementAPI.Models

{
    public enum ChargeStatus
    {
        Free, // ESB
        Success,
        Failed, 
        Waived, // VISA waive checkbox ticked
        
    }
}