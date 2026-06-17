using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TechMove.API.Services;
using TechMoveLogistics.Data;

var builder = WebApplication.CreateBuilder(args);

// Add API controllers and ignore serialization loops for nested relationships
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ENABLES: Outbound Http client factory tracking for our API conversion endpoint
builder.Services.AddHttpClient();

// Register the clean Backend Service Layer
builder.Services.AddScoped<IContractService, ContractService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication Services with solid fallbacks to prevent automated testing startup crashes
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretTechMoveLogisticsKey2026!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechMoveAPI",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "TechMoveMVC",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// FIX: Grab the connection string from appsettings, or fall back to the standard local database name used in Task 2
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=TechMoveLogisticsDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

// Register LogisticsDbContext using the validated connection string
builder.Services.AddDbContext<LogisticsDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Critical: Add Authentication BEFORE Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace TechMove.API
{
    public partial class Program { }
}
