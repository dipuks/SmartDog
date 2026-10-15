using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace SmartDog.Api.Security;

/// <summary>
/// "DecisionApi" configuration. Keys belong in user secrets or environment variables (DecisionApi__ApiKeys__0, …),
/// never in source control. Several keys may be configured to allow rotation.
/// </summary>
public sealed class DecisionApiOptions
{
    public const string SectionName = "DecisionApi";
    public List<string> ApiKeys { get; set; } = [];
    public bool IsConfigured => ApiKeys.Any(k => !string.IsNullOrWhiteSpace(k));
}

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// Service-to-service authentication with the <c>X-Api-Key</c> header. Keys are compared as SHA-256 digests with
/// <see cref="CryptographicOperations.FixedTimeEquals"/>, checking every configured key so timing does not reveal which
/// matched. Key values are never logged. When no key is configured the protected endpoints answer 503, so a
/// misconfigured instance fails closed and visibly.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptionsMonitor<DecisionApiOptions> decisionApi) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    private const string NotConfiguredKey = "SmartDog.ApiKeyNotConfigured";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var keys = decisionApi.CurrentValue.ApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
        if (keys.Count == 0)
        {
            Context.Items[NotConfiguredKey] = true;
            return Task.FromResult(AuthenticateResult.Fail("No API key is configured."));
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var provided) || string.IsNullOrEmpty(provided.ToString()))
            return Task.FromResult(AuthenticateResult.NoResult());

        var providedDigest = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));
        var matched = false;
        foreach (var key in keys)
            matched |= CryptographicOperations.FixedTimeEquals(providedDigest, SHA256.HashData(Encoding.UTF8.GetBytes(key)));

        if (!matched)
        {
            Logger.LogWarning("Rejected decision API request with an invalid API key from {RemoteIp}.", Context.Connection.RemoteIpAddress);
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "service-client")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        var notConfigured = Context.Items.ContainsKey(NotConfiguredKey);
        Response.StatusCode = notConfigured ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status401Unauthorized;
        if (!notConfigured) Response.Headers.WWWAuthenticate = SchemeName;
        await Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = Response.StatusCode,
            Title = notConfigured ? "Decision API is not configured." : "A valid API key is required.",
            Detail = notConfigured
                ? "Set at least one DecisionApi:ApiKeys value (user secrets or environment) to enable this endpoint."
                : $"Send the key in the {HeaderName} header."
        }, options: null, contentType: "application/problem+json");
    }
}
