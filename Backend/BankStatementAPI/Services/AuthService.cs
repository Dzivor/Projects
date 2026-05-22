using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankStatementAPI.Data;
using BankStatementAPI.DTOs;
using BankStatementAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankStatementAPI.Services
{
    public class AuthService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration config, AppDbContext context, ILogger<AuthService> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO request)
        {
            // Strip domain prefix before doing anything
            // Handles: "mbg\daniel.dzivor" or "daniel.dzivor@mbg.local"
            string cleanUsername = request.Username.Trim();

            if (cleanUsername.Contains("\\"))
                cleanUsername = cleanUsername.Split('\\').Last();

            if (cleanUsername.Contains("@"))
                cleanUsername = cleanUsername.Split('@').First();



                _logger.LogInformation("Login attempt for username: {Username}", cleanUsername);

            // Step 1 — Validate against Active Directory
            StaffInfo? staffInfo = ValidateAgainstAD(cleanUsername, request.Password);

            // Step 2 — AD validation failed
            if (staffInfo is null)
            {
                _logger.LogWarning("AD validation failed for username: {Username}", cleanUsername);
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            // Step 3 — Check Users table
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    EF.Functions.Like(u.Username, cleanUsername));

            // Step 4 — Not in Users table
            if (user is null)
            {
                _logger.LogWarning("User not found in database for username: {Username}", cleanUsername);
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Access denied. Please contact IT Admin."
                };
            }
            // Step 5 — Account disabled
            if (!user.IsActive)
            {
                _logger.LogWarning("User account is disabled for username: {Username}", cleanUsername);
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Your account has been disabled. Please contact IT Admin."
                };
            }

            // Step 6 — Authorized — generate token
            _logger.LogInformation("Login successful for username: {Username} (UserId: {UserId})", cleanUsername, user.Id);
            string token = GenerateJwtToken(staffInfo, user.Id);

            return new LoginResponseDTO
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                Username = user.Username,
                FullName = user.FullName,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
        }

        private StaffInfo? ValidateAgainstAD(string username, string password)
        {
            try
            {
                string domain = _config["ActiveDirectory:Domain"]!;

                using var context = new PrincipalContext(ContextType.Domain, domain);

                bool isValid = context.ValidateCredentials(username, password);

                if (!isValid)
                    return null;

                using var user = UserPrincipal.FindByIdentity(
                    context,
                    IdentityType.SamAccountName,
                    username
                );

                if (user is null)
                    return null;

                return new StaffInfo
                {
                    Username = username,
                    FullName = user.DisplayName ?? username,
                    Email = user.EmailAddress ?? ""
                };
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred while validating against Active Directory for username: {Username}", username);
                return null;
            }
        }

        private string GenerateJwtToken(StaffInfo staff, int userId)
        {
            string jwtKey = _config["Jwt:Key"]!;
            string jwtIssuer = _config["Jwt:Issuer"]!;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, staff.Username),
                new Claim(ClaimTypes.GivenName, staff.FullName),
                new Claim(ClaimTypes.Email, staff.Email),
                new Claim("userId", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class StaffInfo
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}