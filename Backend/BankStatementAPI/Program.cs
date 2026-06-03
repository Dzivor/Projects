using BankStatementAPI.Services;
using BankStatementAPI.Middleware;
using BankStatementAPI.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
.MinimumLevel.Information()
//dev? Show everything including debugging
.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
.Enrich.FromLogContext().
WriteTo.Console(
    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
)
// Write to file - essential for prod
.WriteTo.File(
    path: "C:\\Logs\\BankStatementAPI\\log-.txt",
     rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder  = WebApplication.CreateBuilder(args);


builder.Host.UseSerilog();

//Registering services

//controller support
//builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

//Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//calling existing APIs
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();


//Database context
builder.Services.AddDbContext<AppDbContext>((DbContextOptionsBuilder options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));




//custom services
builder.Services.AddScoped<BankApiService>();
builder.Services.AddScoped<ChargingService>();
builder.Services.AddScoped<PdfService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminService>();


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

// Log database connection on startup
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.CanConnect();
    Log.Information("Database connection successful on startup");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Database connection failed on startup");
}

//swagger 
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

var enableHttpsRedirection = builder.Configuration.GetValue<bool?>("UseHttpsRedirection")
    ?? !app.Environment.IsProduction();

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}



//Register our custom auth middleware
app.UseMiddleware<AuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();


//map controllers
app.MapControllers();

//Ensuring logs are flushed before app shuts down
try
{
  app.Run();  
}
finally
{
    Log.CloseAndFlush();
}
