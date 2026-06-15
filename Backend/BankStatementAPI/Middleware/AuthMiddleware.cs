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

            // Let CORS preflight pass through without authentication.
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            if (path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            bool bypassAuthInDevelopment = _config.GetValue<bool>(
                "Auth:BypassInDevelopment"
            );

            if (bypassAuthInDevelopment)
            {
                var env = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>();

                if (env.IsDevelopment())
                {
                    await _next(context);
                    return;
                }
            }
                
            string? authHeader = context.Request.Headers["Authorization"];
            var requestPath = context.Request.Path.Value ?? "";

            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                Serilog.Log.Warning(
                    "Auth rejected: missing/invalid Authorization header. Path={Path}. AuthorizationHeaderPresent={HasAuthHeader}",
                    requestPath,
                    !string.IsNullOrWhiteSpace(authHeader));

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Unauthorized. Please login first."
                });
                return;
            }

            string token = authHeader.Substring("Bearer ".Length).Trim();

            var principal = ValidateToken(token);

            if (principal == null)
            {
                Serilog.Log.Warning(
                    "Auth rejected: invalid/expired JWT. Path={Path}",
                    requestPath);

                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Invalid or expired token. Please login again."
                });
                return;
            }

            // Attach user info to the request
            context.User = principal;

            // Extra debug logging for auth/authorization issues
            string? username = context.User.FindFirstValue(ClaimTypes.Name);
            string? isAdmin = context.User.FindFirstValue("isAdmin");
            Serilog.Log.Information(
                "Auth ok. User={Username}. isAdminClaim={IsAdminClaim}. Path={Path}",
                string.IsNullOrWhiteSpace(username) ? "<missing>" : username,
                isAdmin ?? "<missing>",
                requestPath);

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