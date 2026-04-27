using BankStatementAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankStatementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly BankApiService _bankApiService;

        // ASP.NET automatically injects BankApiService here
        // because we registered it in Program.cs
        public AccountController(BankApiService bankApiService)
        {
            _bankApiService = bankApiService;
        }

        // GET /api/account/lookup/1234567890
        [HttpGet("lookup/{accountNumber}")]
        public async Task<IActionResult> LookupAccount(string accountNumber, [FromQuery] string? channel)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return BadRequest(new { message = "Account number is required" });

            var result = await _bankApiService.GetAccountDetails(accountNumber, channel);

            if (!result.Success)
            {
                if (result.AccountNotFound)
                    return NotFound(new { message = result.Message });

                return StatusCode(503, new { message = result.Message });
            }

            return Ok(result.Account);
        }
    }
}