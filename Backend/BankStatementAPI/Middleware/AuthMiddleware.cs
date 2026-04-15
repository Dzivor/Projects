namespace BankStatementAPI.Middleware
{
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        // RequestDelegate represents the next piece of middleware
        // or the actual endpoint in the pipeline
        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Allow login endpoint through without a token
            // because the user does not have a token yet
            string path = context.Request.Path.Value ?? "";

            if (path.Contains("/api/auth/login"))
            {
                await _next(context);
                return;
            }

            // Check for token in the Authorization header
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

            // Extract the token
            string token = authHeader.Substring("Bearer ".Length);

            // Validate the token
            if (!IsValidToken(token))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Invalid or expired token. Please login again."
                });
                return;
            }

            // Token is valid — allow request through
            await _next(context);
        }

        private bool IsValidToken(string token)
        {
            // We will add full JWT validation here later
            // For now just check it is not empty
            return !string.IsNullOrEmpty(token);
        }
    }
}