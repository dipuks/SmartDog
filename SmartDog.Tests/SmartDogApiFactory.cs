using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SmartDog.Api.Services;
using SmartDog.Application.DTOs;

namespace SmartDog.Tests;

/// <summary>
/// Hosts the real API in memory: environment "Testing" (no user secrets, no development seeding), a per-factory EF
/// in-memory database, a random JWT signing key, test-only decision API keys, and a recording stub in place of the
/// OpenAI endpoint so no test can reach the network.
/// </summary>
public sealed class SmartDogApiFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-primary-key-0123456789abcdef";
    public const string RotatedApiKey = "test-rotated-key-fedcba9876543210";
    private readonly string _database = $"SmartDogTests-{Guid.NewGuid():N}";

    public string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    public bool ConfigureApiKeys { get; init; } = true;
    public RecordingHandler OpenAi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", JwtKey);
        if (ConfigureApiKeys)
        {
            builder.UseSetting("DecisionApi:ApiKeys:0", ApiKey);
            builder.UseSetting("DecisionApi:ApiKeys:1", RotatedApiKey);
        }
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SmartDogDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SmartDogDbContext>>();
            services.AddDbContext<SmartDogDbContext>(options => options.UseInMemoryDatabase(_database));
            services.AddHttpClient<DecisionAssistantService>().ConfigurePrimaryHttpMessageHandler(() => OpenAi);
        });
    }

    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    /// <summary>Registers through the real endpoint and returns a client carrying the issued token.</summary>
    public async Task<(HttpClient Client, Guid UserId)> RegisterAsync(string name)
    {
        var username = $"{name}-{Guid.NewGuid():N}"[..30];
        using var anonymous = CreateHttpsClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, $"Pw-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var userId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Subject);
        return (client, userId);
    }

    /// <summary>Registers a user, promotes it to Admin in the database, and signs in again for a token with that role.</summary>
    public async Task<(HttpClient Client, Guid UserId)> RegisterAdminAsync(string name)
    {
        var username = $"{name}-{Guid.NewGuid():N}"[..30];
        var password = $"Pw-{Guid.NewGuid():N}";
        using var anonymous = CreateHttpsClient();
        (await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, password))).EnsureSuccessStatusCode();
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartDogDbContext>();
            (await db.Users.SingleAsync(user => user.Username == username)).Role = "Admin";
            await db.SaveChangesAsync();
        }
        var response = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return (client, Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Subject));
    }

    /// <summary>A correctly signed, unexpired token with the given claims (for tokens the API itself would never issue).</summary>
    public string CreateToken(params Claim[] claims)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("SmartDog.Api", "SmartDog.Client", claims, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<T> WithDbContextAsync<T>(Func<SmartDogDbContext, Task<T>> work)
    {
        using var scope = Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<SmartDogDbContext>());
    }

    /// <summary>Records outbound calls and answers 503, so any attempt to reach the AI provider is visible.</summary>
    public sealed class RecordingHandler : HttpMessageHandler
    {
        private int _calls;
        public int Calls => _calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }
}
