namespace BankStatementAPI.Models

{
    public enum ChargeStatus
    {
        Free, // ESB
        success,
        failed, 
        waived, // VISA waive checkbox ticked
        
    }
}