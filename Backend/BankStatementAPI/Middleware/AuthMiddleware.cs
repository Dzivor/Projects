using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BankStatementAPI.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;

        public AuthMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;
            _config = config;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string path = context.Request.Path.Value ?? "";

            if (path.Contains("/api/auth/login"))
            {
                await _next(context);
                return;
            }

            // ── TEMPORARY DEV BYPASS ──
            var env = context.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            if (env.IsDevelopment())
            {
                await _next(context);
                return;
            }
            // ── END DEV BYPASS ──
                
            string? authHeader = context.Request.Headers["Authorization"];

            if (string.IsNullOrEmpty(authHeader) ||
                !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Unauthorized. Please login first."
                });
                return;
            }

            string token = authHeader.Substring("Bearer ".Length);

            // ✅ Fixed — using correct method name and return type
            var principal = ValidateToken(token);

            if (principal == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Invalid or expired token. Please login again."
                });
                return;
            }

            // ✅ Fixed — attaching user info to the request
            context.User = principal;

            await _next(context);
        }

        private ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                string jwtKey = _config["Jwt:Key"]!;
                string jwtIssuer = _config["Jwt:Issuer"]!;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                return tokenHandler.ValidateToken(
                    token,
                    validationParameters,
                    out _
                );
            }
            catch
            {
                return null;
            }
        }
    }
}