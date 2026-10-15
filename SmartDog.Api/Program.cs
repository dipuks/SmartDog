using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using SmartDog.Api.Middleware;
using SmartDog.Api.Security;
using SmartDog.Api.Services;
using SmartDog.Api.Workers;
using SmartDog.Domain.Engines;
using SmartDog.Application.Configuration;
using SmartDog.Application.Services;
using SmartDog.Domain.Models;
using SmartDog.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConcurrencyExceptionHandler>();
builder.Services.AddExceptionHandler<MissingUserExceptionHandler>();

// Every decision read or write is scoped to the signed-in user (the JWT subject).
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.AddDbContext<SmartDogDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SmartDogDatabase")));

builder.Services.AddScoped<
    IDecisionQueryService,
    DecisionQueryService>();

builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();

builder.Services.AddScoped<
    IUserRepository,
    UserRepository>();

builder.Services.AddScoped<
    IDecisionCommandService,
    DecisionCommandService>();

builder.Services.AddScoped<
    IAuthenticationService,
    AuthenticationService>();

builder.Services.AddScoped<
    IPasswordHasher<User>,
    PasswordHasher<User>>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("ai-assistant", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHttpClient<DecisionAssistantService>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "JWT key must be at least 32 bytes long.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddScoped<
    IDecisionAlternativeRepository,
    DecisionAlternativeRepository>();

builder.Services.AddScoped<
    IDecisionAlternativeService,
    DecisionAlternativeService>();

builder.Services.AddScoped<
    IDecisionCriterionRepository,
    DecisionCriterionRepository>();
builder.Services.AddScoped<
    IDecisionCriterionService,
    DecisionCriterionService>();
builder.Services.AddScoped<
    IDecisionAlternativeRatingService,
    DecisionAlternativeRatingService>();
builder.Services.AddScoped<
    IDecisionScoringService,
    DecisionScoringService>();
builder.Services.AddScoped<
    IDecisionScoringEngine,
    WeightedDecisionScoringEngine>();

builder.Services.AddMemoryCache();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SmartDogDbContext>();

builder.Services.AddHostedService<DecisionWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var seedPassword = app.Configuration["SeedAdmin:Password"];

    if (!string.IsNullOrWhiteSpace(seedPassword))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<SmartDogDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        await DbSeeder.SeedAsync(
            dbContext,
            passwordHasher,
            app.Configuration["SeedAdmin:Username"] ?? "admin",
            seedPassword);
    }
}

app.UseExceptionHandler();

app.UseCors("AngularClient");

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program;
