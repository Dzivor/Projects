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

builder.Services.AddHttpClient();//calling existing APIs

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
        policy.WithOrigins("http://localhost:5173") // Replace with your frontend URL
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
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

//CORS

app.UseCors("AllowFrontend");

//Register our custom auth middleware
app.UseMiddleware<AuthMiddleware>();

app.UseAuthentication();
app.UseAuthorization();


//map controllers
app.MapControllers();


app.Run();