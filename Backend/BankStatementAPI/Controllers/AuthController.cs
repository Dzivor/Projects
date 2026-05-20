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
            // Validate inputs before hitting AD or database
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return Ok(new LoginResponseDTO
                {
                    Success = false,
                    Message = "Username and password are required"
                });
            }

            // Login always returns a LoginResponseDTO
            // never throws — all errors handled inside AuthService
            var result = await _authService.Login(request);

            // Always return 200 — frontend reads Success field
            // to determine what to do next
            return Ok(result);
        }
    }
}