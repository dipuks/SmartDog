using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SmartDog.Application.DTOs;
using SmartDog.Domain.Models;

namespace SmartDog.Tests;

/// <summary>
/// Per-user ownership through the real pipeline (JWT, routing, services, EF): a decision and everything under it is
/// visible to and changeable by its owner only. Every other user, administrators included, gets the same 404 as for a
/// decision that does not exist.
/// </summary>
public sealed class DecisionOwnershipApiTests(OwnershipFixture fixture) : IClassFixture<OwnershipFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly byte[] AnyRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    public static TheoryData<string, string> EveryDecisionEndpoint => new()
    {
        { "GET", "/api/decisions/{d}" },
        { "PUT", "/api/decisions/{d}" },
        { "GET", "/api/decisions/{d}/results" },
        { "GET", "/api/decisions/{d}/criteria" },
        { "POST", "/api/decisions/{d}/criteria" },
        { "GET", "/api/decisions/{d}/criteria/{c}" },
        { "PUT", "/api/decisions/{d}/criteria/{c}" },
        { "DELETE", "/api/decisions/{d}/criteria/{c}" },
        { "GET", "/api/decisions/{d}/alternatives" },
        { "POST", "/api/decisions/{d}/alternatives" },
        { "GET", "/api/decisions/{d}/alternatives/{a}" },
        { "PUT", "/api/decisions/{d}/alternatives/{a}" },
        { "DELETE", "/api/decisions/{d}/alternatives/{a}" },
        { "GET", "/api/decisions/{d}/alternatives/{a}/ratings" },
        { "PUT", "/api/decisions/{d}/alternatives/{a}/ratings/{c}" },
        { "POST", "/api/decisions/{d}/assistant" }
    };

    [Theory]
    [MemberData(nameof(EveryDecisionEndpoint))]
    public async Task Another_user_gets_the_same_404_as_for_a_decision_that_does_not_exist(string method, string template)
    {
        await fixture.Alice.GetAsync($"/api/decisions/{fixture.DecisionId}"); // the owner's read is cached first
        var aiCallsBefore = fixture.Factory.OpenAi.Calls;

        var forOwnedByAlice = await SendAsync(fixture.Bob, method, Url(template, fixture.DecisionId, fixture.CriterionId, fixture.AlternativeId));
        var forMissing = await SendAsync(fixture.Bob, method, Url(template, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, forOwnedByAlice.StatusCode);
        Assert.Equal(forMissing.StatusCode, forOwnedByAlice.StatusCode);
        Assert.Equal(await BodyWithoutTraceIdAsync(forMissing), await BodyWithoutTraceIdAsync(forOwnedByAlice));
        Assert.Equal(aiCallsBefore, fixture.Factory.OpenAi.Calls); // nothing about Alice's decision was sent to the AI provider
        await fixture.AssertAliceDataUnchangedAsync();
    }

    [Theory]
    [MemberData(nameof(EveryDecisionEndpoint))]
    public async Task Administrators_get_404_for_decisions_they_do_not_own(string method, string template)
    {
        var response = await SendAsync(fixture.Admin, method, Url(template, fixture.DecisionId, fixture.CriterionId, fixture.AlternativeId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await fixture.AssertAliceDataUnchangedAsync();
    }

    [Theory]
    [MemberData(nameof(EveryDecisionEndpoint))]
    public async Task The_owner_reaches_every_endpoint(string method, string template)
    {
        if (method != "GET" && template != "/api/decisions/{d}/assistant") return; // writes are covered below without changing shared data

        var response = await SendAsync(fixture.Alice, method, Url(template, fixture.DecisionId, fixture.CriterionId, fixture.AlternativeId));

        // The assistant answers 502 here because the AI provider is a stub: the request got past the ownership check.
        Assert.Equal(template.EndsWith("/assistant") ? HttpStatusCode.BadGateway : HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_owner_can_change_and_delete_what_others_cannot()
    {
        var (owner, _) = await fixture.Factory.RegisterAsync("carol");
        var decision = await CreateDecisionAsync(owner, "Carol's decision");
        var criterion = await (await owner.PostAsJsonAsync($"/api/decisions/{decision}/criteria", new { name = "Speed", weight = 100 })).Content.ReadFromJsonAsync<DecisionCriterionDto>(Json);
        var alternative = await (await owner.PostAsJsonAsync($"/api/decisions/{decision}/alternatives", new { name = "Fast", description = (string?)null, score = 0 })).Content.ReadFromJsonAsync<DecisionAlternativeDto>(Json);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/decisions/{decision}/alternatives/{alternative!.Id}/ratings/{criterion!.Id}", new { rating = 70 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/decisions/{decision}/criteria/{criterion.Id}", new { name = "Speed", weight = 100 })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/decisions/{decision}/alternatives/{alternative.Id}", new { name = "Faster", description = (string?)null, score = 0 })).StatusCode);
        var results = await owner.GetFromJsonAsync<List<DecisionResultDto>>($"/api/decisions/{decision}/results", Json);
        Assert.Equal(70m, Assert.Single(results!).Score);
        // The in-memory test database does not generate SQL Server rowversions, so the owner's update ends in the
        // concurrency check (409), past the ownership check; other users never get that far (404).
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync($"/api/decisions/{decision}", new { title = "Renamed", description = (string?)null, status = "InProgress", rowVersion = AnyRowVersion })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/decisions/{decision}/alternatives/{alternative.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/decisions/{decision}/criteria/{criterion.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_needs_the_admin_role_and_ownership()
    {
        var foreign = await fixture.Bob.DeleteAsync($"/api/decisions/{fixture.DecisionId}");
        var notOwned = await fixture.Admin.DeleteAsync($"/api/decisions/{fixture.DecisionId}");
        var adminsOwn = await CreateDecisionAsync(fixture.Admin, "Admin's decision");
        var owned = await fixture.Admin.DeleteAsync($"/api/decisions/{adminsOwn}");

        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode); // the role check comes first and reveals nothing
        Assert.Equal(HttpStatusCode.NotFound, notOwned.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, owned.StatusCode);
        await fixture.AssertAliceDataUnchangedAsync();
    }

    [Fact]
    public async Task Lists_contain_only_the_callers_decisions()
    {
        var bobsDecision = await CreateDecisionAsync(fixture.Bob, "Bob's decision");

        var alices = await fixture.Alice.GetFromJsonAsync<PagedResult<DecisionDto>>("/api/decisions?pageSize=100", Json);
        var bobs = await fixture.Bob.GetFromJsonAsync<PagedResult<DecisionDto>>("/api/decisions?pageSize=100", Json);
        var admins = await fixture.Admin.GetFromJsonAsync<PagedResult<DecisionDto>>("/api/decisions?pageSize=100", Json);

        Assert.Contains(alices!.Items, d => d.Id == fixture.DecisionId);
        Assert.DoesNotContain(alices.Items, d => d.Id == bobsDecision);
        Assert.Contains(bobs!.Items, d => d.Id == bobsDecision);
        Assert.DoesNotContain(bobs.Items, d => d.Id == fixture.DecisionId);
        Assert.DoesNotContain(admins!.Items, d => d.Id == fixture.DecisionId || d.Id == bobsDecision);
        Assert.Equal(bobs.Items.Count, bobs.TotalCount);
    }

    [Fact]
    public async Task An_owner_in_the_request_body_is_ignored()
    {
        var response = await fixture.Alice.PostAsJsonAsync("/api/decisions", new { title = "Forged owner", description = (string?)null, ownerId = fixture.BobId });
        var created = (await response.Content.ReadFromJsonAsync<DecisionDto>(Json))!;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(fixture.AliceId, await fixture.Factory.WithDbContextAsync(db => db.Decisions.Where(d => d.Id == created.Id).Select(d => d.OwnerId).SingleAsync()));
        Assert.Equal(HttpStatusCode.NotFound, (await fixture.Bob.GetAsync($"/api/decisions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Alice.GetAsync($"/api/decisions/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Decisions_without_an_owner_are_visible_to_nobody()
    {
        var legacyId = await fixture.Factory.WithDbContextAsync(async db =>
        {
            var legacy = new Decision { Id = Guid.NewGuid(), Title = "Created before ownership", CreatedAt = DateTime.UtcNow, Status = "Created" };
            db.Decisions.Add(legacy);
            await db.SaveChangesAsync();
            return legacy.Id;
        });

        foreach (var client in new[] { fixture.Alice, fixture.Bob, fixture.Admin })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/decisions/{legacyId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/decisions/{legacyId}/criteria")).StatusCode);
            Assert.DoesNotContain((await client.GetFromJsonAsync<PagedResult<DecisionDto>>("/api/decisions?pageSize=100", Json))!.Items, d => d.Id == legacyId);
        }
    }

    [Fact]
    public async Task A_valid_token_without_a_user_id_gets_401_and_no_data()
    {
        using var client = fixture.Factory.CreateHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            fixture.Factory.CreateToken(new Claim(ClaimTypes.Name, "no-subject"), new Claim(ClaimTypes.Role, "Admin")));

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/decisions")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"/api/decisions/{fixture.DecisionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/decisions", new { title = "No owner" })).StatusCode);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        using var anonymous = fixture.Factory.CreateHttpsClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/decisions/{fixture.DecisionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/decisions")).StatusCode);
    }

    private static string Url(string template, Guid decision, Guid criterion, Guid alternative) =>
        template.Replace("{d}", decision.ToString()).Replace("{c}", criterion.ToString()).Replace("{a}", alternative.ToString());

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        object? body = (method, url) switch
        {
            ("PUT", _) when url.Contains("/ratings/") => new { rating = 10 },
            ("PUT", _) when url.Contains("/criteria/") => new { name = "Changed", weight = 50 },
            ("PUT", _) when url.Contains("/alternatives/") => new { name = "Changed", description = (string?)null, score = 1 },
            ("PUT", _) => new { title = "Changed by someone else", description = (string?)null, status = "InProgress", rowVersion = AnyRowVersion },
            ("POST", _) when url.EndsWith("/criteria") => new { name = "Injected", weight = 10 },
            ("POST", _) when url.EndsWith("/alternatives") => new { name = "Injected", description = (string?)null, score = 1 },
            ("POST", _) when url.EndsWith("/assistant") => new { question = "What should I choose?" },
            _ => null
        };
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        if (url.EndsWith("/assistant")) request.Headers.Add("X-OpenAI-Api-Key", "placeholder-openai-key-for-tests");
        return client.SendAsync(request);
    }

    private static async Task<string> BodyWithoutTraceIdAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(text)) return text;
        var node = JsonNode.Parse(text)!.AsObject();
        node.Remove("traceId");
        return node.ToJsonString();
    }

    private static async Task<Guid> CreateDecisionAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/api/decisions", new { title, description = (string?)null });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DecisionDto>(Json))!.Id;
    }
}

/// <summary>Alice owns one decision with a criterion, an alternative and a rating; Bob and an administrator own none of it.</summary>
public sealed class OwnershipFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public SmartDogApiFactory Factory { get; } = new();
    public HttpClient Alice { get; private set; } = null!;
    public HttpClient Bob { get; private set; } = null!;
    public HttpClient Admin { get; private set; } = null!;
    public Guid AliceId { get; private set; }
    public Guid BobId { get; private set; }
    public Guid DecisionId { get; private set; }
    public Guid CriterionId { get; private set; }
    public Guid AlternativeId { get; private set; }

    public async Task InitializeAsync()
    {
        (Alice, AliceId) = await Factory.RegisterAsync("alice");
        (Bob, BobId) = await Factory.RegisterAsync("bob");
        (Admin, _) = await Factory.RegisterAdminAsync("admin");

        var decision = await Alice.PostAsJsonAsync("/api/decisions", new { title = "Choose a database", description = "Alice's private decision" });
        DecisionId = (await decision.Content.ReadFromJsonAsync<DecisionDto>(Json))!.Id;
        var criterion = await Alice.PostAsJsonAsync($"/api/decisions/{DecisionId}/criteria", new { name = "Cost", weight = 100 });
        CriterionId = (await criterion.Content.ReadFromJsonAsync<DecisionCriterionDto>(Json))!.Id;
        var alternative = await Alice.PostAsJsonAsync($"/api/decisions/{DecisionId}/alternatives", new { name = "PostgreSQL", description = (string?)null, score = 0 });
        AlternativeId = (await alternative.Content.ReadFromJsonAsync<DecisionAlternativeDto>(Json))!.Id;
        (await Alice.PutAsJsonAsync($"/api/decisions/{DecisionId}/alternatives/{AlternativeId}/ratings/{CriterionId}", new { rating = 80 })).EnsureSuccessStatusCode();
    }

    /// <summary>Alice's decision and everything under it are exactly as created.</summary>
    public async Task AssertAliceDataUnchangedAsync()
    {
        var decision = await Alice.GetFromJsonAsync<DecisionDto>($"/api/decisions/{DecisionId}", Json);
        var criteria = await Alice.GetFromJsonAsync<List<DecisionCriterionDto>>($"/api/decisions/{DecisionId}/criteria", Json);
        var alternatives = await Alice.GetFromJsonAsync<List<DecisionAlternativeDto>>($"/api/decisions/{DecisionId}/alternatives", Json);
        var ratings = await Alice.GetFromJsonAsync<List<DecisionAlternativeRatingDto>>($"/api/decisions/{DecisionId}/alternatives/{AlternativeId}/ratings", Json);

        Assert.Equal(("Choose a database", "Created"), (decision!.Title, decision.Status));
        Assert.Equal(("Cost", 100m), (Assert.Single(criteria!).Name, criteria![0].Weight));
        Assert.Equal("PostgreSQL", Assert.Single(alternatives!).Name);
        Assert.Equal(80m, Assert.Single(ratings!).Rating);
    }

    public Task DisposeAsync()
    {
        Alice.Dispose();
        Bob.Dispose();
        Admin.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }
}
