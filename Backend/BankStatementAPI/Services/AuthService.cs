using System.DirectoryServices.AccountManagement;
using System.IdentityModel.Tokens.Jwt;
using System.Data.Common;
using System.Security.Claims;
using System.Text;
using BankStatementAPI.DTOs;
using BankStatementAPI.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;

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

        public LoginResponseDTO? Login(LoginRequestDTO request)
        {
            string username = request.Username.Trim();

            // Step 1 — Validate against Active Directory
            var staffInfo = ValidateAgainstAD(
                username,
                request.Password
            );

            // Step 2 — If AD validation failed throw unauthorized error
            if (staffInfo == null)
                throw new UnauthorizedAccessException("Invalid username or password");

            // Step 3 — Make sure the user is registered in BankStatementDB.Users
            if (!IsRegisteredUser(username))
                throw new UnauthorizedAccessException("Invalid username or password");

            // Step 4 — Generate JWT token
            string token = GenerateJwtToken(staffInfo);

            return new LoginResponseDTO
            {
                Token = token,
                Username = staffInfo.Username,
                FullName = staffInfo.FullName,
                ExpiresAt = DateTime.Now.AddHours(8) // token valid for 8 hours
            };
        }

        private bool IsRegisteredUser(string username)
        {
            try
            {
                var connection = _context.Database.GetDbConnection();
                bool shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;

                if (shouldCloseConnection)
                    _context.Database.OpenConnection();

                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText =
                        "SELECT TOP (1) 1 FROM [Users] WHERE [Username] = @username";

                    DbParameter parameter = command.CreateParameter();
                    parameter.ParameterName = "@username";
                    parameter.Value = username;
                    command.Parameters.Add(parameter);

                    return command.ExecuteScalar() != null;
                }
                finally
                {
                    if (shouldCloseConnection)
                        _context.Database.CloseConnection();
                }
            }
            catch
            {
                return false;
            }
        }

        private StaffInfo? ValidateAgainstAD(string username, string password)
        {
            try
            {
                string domain = _config["ActiveDirectory:Domain"]!;

                // PrincipalContext connects to your company's AD
                using var context = new PrincipalContext(
                    ContextType.Domain,
                    domain
                );

                // ValidateCredentials checks username + password against AD
                bool isValid = context.ValidateCredentials(username, password);

                if (!isValid)
                    return null;

                // Get the user's full details from AD
                using var user = UserPrincipal.FindByIdentity(
                    context,
                    IdentityType.SamAccountName,
                    username
                );

                if (user == null)
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
                // If AD is unreachable or any error occurs
                return null;
            }
        }

        private string GenerateJwtToken(StaffInfo staff)
        {
            string jwtKey = _config["Jwt:Key"]!;
            string jwtIssuer = _config["Jwt:Issuer"]!;

            // Claims are pieces of information stored inside the token
            // The frontend can read these without calling the backend
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, staff.Username),
                new Claim(ClaimTypes.GivenName, staff.FullName),
                new Claim(ClaimTypes.Email, staff.Email),
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()) // unique token ID
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Internal class to hold AD user info
    public class StaffInfo
    {
        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
    }
}