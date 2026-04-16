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
        public IActionResult Login([FromBody] LoginRequestDTO request)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new
                {
                    message = "Username and password are required"
                });
            }

            var result = _authService.Login(request);

            // Login failed — wrong credentials or not in AD
            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password"
                });
            }

            return Ok(result);
        }
    }
}