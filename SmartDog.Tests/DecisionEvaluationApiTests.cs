using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SmartDog.Tests;

/// <summary>
/// The stateless evaluation API (<c>POST /api/decisions/evaluate</c>) through the real pipeline: API-key
/// authentication, routing, size limit, validation and JSON. This is the contract CQ Pulse calls.
/// </summary>
public sealed class DecisionEvaluationApiTests : IDisposable
{
    private readonly SmartDogApiFactory _factory = new();

    private static readonly object ValidRequest = new
    {
        question = "Which investigation first?",
        criteria = new object[]
        {
            new { id = "impact", name = "Impact", weight = 3, direction = "maximize" },
            new { id = "effort", name = "Effort", weight = 1, direction = "minimize" }
        },
        options = new object[]
        {
            new { id = "a", name = "Option A", values = new Dictionary<string, double> { ["impact"] = 10, ["effort"] = 5 } },
            new { id = "b", name = "Option B", values = new Dictionary<string, double> { ["impact"] = 6, ["effort"] = 1 } }
        }
    };

    public void Dispose() => _factory.Dispose();

    private HttpClient Client(string? key = null)
    {
        var client = _factory.CreateHttpsClient();
        if (key is not null) client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }

    [Fact]
    public async Task Valid_key_returns_a_ranking_with_string_status_and_explanations()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);

        var response = await client.PostAsJsonAsync("/api/decisions/evaluate", ValidRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("Ranked", root.GetProperty("status").GetString());
        Assert.Equal("a", root.GetProperty("ranking")[0].GetProperty("optionId").GetString());
        Assert.Equal(0.75, root.GetProperty("ranking")[0].GetProperty("score").GetDouble());
        Assert.Equal(2, root.GetProperty("ranking")[0].GetProperty("contributions").GetArrayLength());
        Assert.Contains("Strongest", root.GetProperty("ranking")[0].GetProperty("explanation").GetString());
        Assert.Contains("not a prediction", root.GetProperty("limitations").GetString());
        Assert.True(Guid.TryParse(root.GetProperty("evaluationId").GetString(), out _));
    }

    [Fact]
    public async Task Rotated_keys_are_accepted()
    {
        using var client = Client(SmartDogApiFactory.RotatedApiKey);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/decisions/evaluate", ValidRequest)).StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-key")]
    [InlineData(SmartDogApiFactory.ApiKey + "x")]
    public async Task Missing_or_wrong_keys_are_rejected(string? key)
    {
        using var client = Client(key);

        var response = await client.PostAsJsonAsync("/api/decisions/evaluate", ValidRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("ApiKey", response.Headers.WwwAuthenticate.ToString());
        Assert.DoesNotContain(SmartDogApiFactory.ApiKey, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task User_tokens_are_not_accepted_by_the_evaluation_api()
    {
        var (user, _) = await _factory.RegisterAsync("user");

        var response = await user.PostAsJsonAsync("/api/decisions/evaluate", ValidRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        user.Dispose();
    }

    [Fact]
    public async Task Api_keys_are_not_accepted_by_the_user_endpoints()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/decisions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/decisions", new { title = "Via API key" })).StatusCode);
    }

    [Fact]
    public async Task Without_configured_keys_the_endpoint_fails_closed_with_503()
    {
        using var unconfigured = new SmartDogApiFactory { ConfigureApiKeys = false };
        using var client = unconfigured.CreateHttpsClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", SmartDogApiFactory.ApiKey);

        var response = await client.PostAsJsonAsync("/api/decisions/evaluate", ValidRequest);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("DecisionApi:ApiKeys", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Bodies_over_the_size_limit_are_rejected_before_evaluation()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);
        var padding = new string('x', 300 * 1024);
        var content = new StringContent($$"""{"question":"{{padding}}","criteria":[],"options":[]}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/decisions/evaluate", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_requests_return_400_with_field_errors()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);

        var response = await client.PostAsJsonAsync("/api/decisions/evaluate", new
        {
            criteria = new object[] { new { id = "impact", weight = 0 } },
            options = new object[] { new { id = "a", values = new Dictionary<string, double> { ["other"] = 1, ["impact"] = 2e15 } } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = json.RootElement.GetProperty("errors").EnumerateObject().Select(e => e.Name).ToList();
        Assert.Contains("criteria[0].weight", errors);
        Assert.Contains("options[0].values.other", errors);
        Assert.Contains("options[0].values.impact", errors);
    }

    [Fact]
    public async Task Malformed_json_is_rejected_before_evaluation()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);

        var response = await client.PostAsync("/api/decisions/evaluate", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Infeasible_constraints_are_reported_with_200_and_no_ranking()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);

        var response = await client.PostAsJsonAsync("/api/decisions/evaluate", new
        {
            criteria = new object[] { new { id = "impact", weight = 1 } },
            options = new object[] { new { id = "a", values = new Dictionary<string, double> { ["impact"] = 1 } } },
            constraints = new object[] { new { id = "min-impact", criterionId = "impact", @operator = ">=", value = 5 } }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("NoFeasibleOption", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, json.RootElement.GetProperty("ranking").GetArrayLength());
        Assert.Equal("impact = 1 violates >= 5 (constraint 'min-impact').", // unnamed criterion: shown by id
            json.RootElement.GetProperty("infeasible")[0].GetProperty("violations")[0].GetString());
    }

    [Fact]
    public async Task Correlation_ids_are_echoed_and_invalid_ones_replaced()
    {
        using var client = Client(SmartDogApiFactory.ApiKey);
        var valid = new HttpRequestMessage(HttpMethod.Post, "/api/decisions/evaluate") { Content = JsonContent.Create(ValidRequest) };
        valid.Headers.Add("X-Correlation-ID", "cqpulse-test-0001");
        var invalid = new HttpRequestMessage(HttpMethod.Post, "/api/decisions/evaluate") { Content = JsonContent.Create(ValidRequest) };
        invalid.Headers.Add("X-Correlation-ID", "bad id with spaces");

        var echoed = (await client.SendAsync(valid)).Headers.GetValues("X-Correlation-ID").Single();
        var replaced = (await client.SendAsync(invalid)).Headers.GetValues("X-Correlation-ID").Single();

        Assert.Equal("cqpulse-test-0001", echoed);
        Assert.Matches("^[0-9a-f]{32}$", replaced);
    }

    [Fact]
    public async Task Health_is_anonymous()
    {
        using var client = Client();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
