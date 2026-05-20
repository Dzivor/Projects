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

        public AuthService(IConfiguration config, AppDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<LoginResponseDTO> Login(LoginRequestDTO request)
        {
            // Step 1 — Validate against Active Directory
            StaffInfo? staffInfo = ValidateAgainstAD(
                request.Username,
                request.Password
            );

            // Step 2 — AD validation failed
            if (staffInfo is null)
            {
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Step 3 — Check Users table
            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == request.Username.ToLower());

            // Step 4 — Not in Users table
            if (user is null)
            {
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Unauthorized access. Contact IT admin for access."
                };
            }

            // Step 5 — In table but disabled
            if (!user.IsActive)
            {
                return new LoginResponseDTO
                {
                    Success = false,
                    Message = "Unauthorized access. Contact IT admin for access."
                };
            }

            // Step 6 — Authorized — generate token
            string token = GenerateJwtToken(staffInfo, user.Id);

            return new LoginResponseDTO
            {
                Success = true,
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

                using var context = new PrincipalContext(
                    ContextType.Domain,
                    domain
                );

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
            catch
            {
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
                // Store userId in token so controllers can access it
                new Claim("userId", userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

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

    // ─────────────────────────────────────────
    // StaffInfo — internal class
    // Holds AD user details temporarily
    // during the login process
    // ─────────────────────────────────────────
    public class StaffInfo
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}