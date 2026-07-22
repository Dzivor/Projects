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
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
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
            catch (Exception ex)
            {
                // Log the exception here when Serilog is added
                _logger.LogError(ex,
                 "An error occurred while processing login request for username: {Username}", 
                 request.Username);
                return Ok(new LoginResponseDTO
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                });
            }
        }
    }
}