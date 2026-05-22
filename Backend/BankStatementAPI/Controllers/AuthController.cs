using BankStatementAPI.DTOs;
using BankStatementAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankStatementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return Ok(new LoginResponseDTO
                {
                    Success = false,
                    Message = "Username and password are required."
                });
            }

            try
            {
                var result = await _authService.Login(request);

                // Always return 200
                // Frontend reads result.Success to determine what to do
                return Ok(result);
            }
            catch (Exception)
            {
                // Log the exception here when Serilog is added
                return Ok(new LoginResponseDTO
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                });
            }
        }
    }
}