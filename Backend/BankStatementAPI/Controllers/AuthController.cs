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

        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new LoginResponseDTO
                {
                    Success = false,
                    Message = "Username and password are required."
                });
            }

            var result = await _authService.Login(request);

            // Return proper HTTP status codes
            if (!result.Success)
                return Unauthorized(new LoginResponseDTO
                {
                    Success = false,
                    Message = result.Message
                });

            return Ok(result);
        }
    }
}