using BankStatementAPI.Services;
using BankStatementAPI.Middleware;
using BankStatementAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder  = WebApplication.CreateBuilder(args);

//Registering services

//controller support
builder.Services.AddControllers();


//Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//calling existing APIs
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();


//Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


//custom services
builder.Services.AddScoped<BankApiService>();
builder.Services.AddScoped<ChargingService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();


//frontend connection
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? Array.Empty<string>();

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


//build

var app= builder.Build();

//swagger in development environment only
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.CanConnect())
    {
        Console.WriteLine("Database connection successful");
    }else
    {
        Console.WriteLine("Database connection failed");
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

//CORS

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();



//Register our custom auth middleware
app.UseMiddleware<AuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();


//map controllers
app.MapControllers();


app.Run();