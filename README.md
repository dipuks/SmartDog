# SmartDog

SmartDog helps people make considered choices by comparing alternatives against weighted criteria. Create a decision, list the options, choose what matters, rate each option, and review a transparent weighted ranking. An optional AI assistant can suggest criteria and alternatives or explain the calculated results without changing the user's ratings or decision.

The repository contains an ASP.NET Core REST API and an Angular single-page client. Users can register and sign in, create and manage decisions, compare alternatives, and view ranked results. Each decision belongs to the user who created it. The API uses SQL Server persistence and JWT bearer authentication. The optional OpenAI assistant accepts a user-provided key in the UI; the key is not saved by the app.

For other services, a stateless [evaluation API](#evaluation-api-for-service-clients) ranks submitted options with the same kind of transparent weighted method, authenticated with an API key. [CQ Pulse](https://github.com/dipuks/cq-pulse) uses it to rank a test run's failure signatures.

## Tech stack

| Area | Technologies |
| --- | --- |
| Frontend | Angular 22, TypeScript 6, RxJS, HTML, CSS |
| Backend API | ASP.NET Core / .NET 10, C# |
| Application architecture | Domain, Application, Infrastructure, API layers |
| Persistence | Entity Framework Core 10, SQL Server, EF Core migrations |
| Authentication | JWT bearer tokens, ASP.NET Core password hashing |
| Optional AI | OpenAI Chat Completions API, user-provided session-only API key |
| Testing and CI | xUnit, Angular unit tests with Vitest, GitHub Actions |

## Prerequisites

- .NET 10 SDK
- SQL Server LocalDB or another SQL Server instance
- Node.js 24.15.0 or later in the Node 24 line and npm (for the Angular client)

## Local configuration

The checked-in `SmartDog.Api/appsettings.json` selects `(localdb)\MSSQLLocalDB` and contains non-secret JWT issuer and audience defaults. The API requires a JWT signing key of at least 32 bytes. Store it in .NET User Secrets rather than in a checked-in settings file:

```powershell
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-secret-at-least-32-bytes-long" --project SmartDog.Api
```

To use a different local SQL Server, set `ConnectionStrings:SmartDogDatabase` in User Secrets or the environment. Before running any database command, verify that this setting targets the intended **development** database; the API does not apply migrations automatically.

For development-only admin seeding, configure `SeedAdmin:Password` (and optionally `SeedAdmin:Username`) in User Secrets. Seeding only runs in the Development environment and only when the database has no users.

## How to use

1. Start the API and Angular client using the commands below, then open `http://localhost:4200`.
2. Create an account from the sign-in page, or sign in with a development account that has already been seeded.
3. Create a decision with a descriptive title and optional context.
4. Open it, add alternatives, and add criteria with importance weights that total exactly 100%.
5. Rate every alternative from 0 to 100 for each criterion. SmartDog calculates a weighted score and ranks the options.
6. Optionally enter your own OpenAI API key in the decision assistant to ask for suggestions or an explanation of the ranking. The key remains in memory for the current browser session; clear it or sign out to remove it. AI use can incur charges on the user's OpenAI account.

Registration requires a username of 3–100 letters, numbers, periods, underscores, or hyphens and a password of at least 12 characters. New accounts are signed in automatically. Email verification and password recovery are not currently implemented.

## Authorization model

| Endpoint | Who | Rule |
| --- | --- | --- |
| `POST /api/auth/register`, `POST /api/auth/login` | Anyone | Rate-limited per client IP |
| `/api/decisions` and everything under `/api/decisions/{id}`: criteria, alternatives, ratings, results, assistant | Signed-in users (JWT) | A decision belongs to the user who created it. Every read and write, including nested resources and the assistant, is scoped to that owner. Another user's decision gets the same 404 as one that does not exist. The owner comes from the token, never from the request body. |
| `DELETE /api/decisions/{id}` | `Admin` role, own decisions only | Administrators do not see or change other users' decisions |
| `POST /api/decisions/evaluate` | Service clients | `X-Api-Key` only: user JWTs are not accepted there, and API keys are not accepted anywhere else |
| `/health` | Anyone | Includes the database check |

Decisions created before ownership existed (migration `AddDecisionOwnership`) have no owner and are visible to nobody until one is assigned; see [Database migrations](#database-migrations).

## AI decision assistant

The authenticated decision workspace includes an optional OpenAI-powered assistant. Users can enter their own key in the assistant UI; no provider key is embedded in the app or required on the server. The key is held only in client memory, sent to the API over HTTPS for assistant requests, and cleared on refresh, tab close, or sign-out. It is not written to browser storage or application logs. Assistant calls are limited to 20 per authenticated user per hour in the API process.

When a user asks the assistant, the API forwards that decision's title, description, criteria and weights, alternatives and descriptions, ratings, calculated ranking when complete, and the user's question to OpenAI. Do not include sensitive information in decision data unless you are comfortable sending it to OpenAI. Responses can be incorrect and should be treated as suggestions, not advice.

## Evaluation API for service clients

`POST /api/decisions/evaluate` ranks options by weighted criteria and returns the ranking with per-criterion contributions and explanations. It is stateless (nothing is stored), deterministic, and involves no AI or machine learning.

**Configuration.** Set at least one key; several keys allow rotation. Without a key the endpoint answers 503, so a misconfigured instance fails closed. Keys are compared as SHA-256 digests in constant time and are never logged.

```powershell
dotnet user-secrets set "DecisionApi:ApiKeys:0" "replace-with-a-long-random-key" --project SmartDog.Api
```

A client such as CQ Pulse then uses `Integrations:SmartDog:BaseUrl = https://localhost:7259` and the same value as `Integrations:SmartDog:ApiKey`.

**Request.** At most 256 KB.

| Field | Rules |
| --- | --- |
| `question` | Optional, up to 500 characters |
| `criteria[]`: `id`, `name`, `weight`, `direction` | 1–20 criteria. `id` is 1–64 letters, digits, `.`, `_` or `-` and unique. `weight` is greater than 0 and at most 1000 (weights are relative). `direction` is `maximize` (default) or `minimize` |
| `options[]`: `id`, `name`, `values` | 1–200 options with unique ids. `values` maps every criterion id to a finite number between -1e15 and 1e15 |
| `constraints[]`: `id`, `criterionId`, `operator`, `value` | Up to 50 hard constraints. `operator` is one of `<`, `<=`, `>`, `>=`, `==`, `!=` |

**Method.** Options that violate a constraint are excluded. Each criterion is scaled to 0–1 across the remaining options (1 = best). A criterion whose values are all equal scales to 1 for every option and cannot change the order. Values count as equal when they differ by at most 1e-12 of their magnitude, which is floating-point noise; constraint operators use the same equality, so `<=` and `>=` agree with `==`. Weights are divided by their sum, and the score is the weighted sum of the scaled values (0–1). Scores are ranked at a resolution of 1e-9. Options whose scores round to the same step are ties: they are ordered by option id, reported in `tiedWithOptionIds`, and given the same score. Reported scores are rounded to 6 decimals.

**Responses.**

| Status | Meaning |
| --- | --- |
| 200 | `status` is `Ranked`, or `NoFeasibleOption` with every option and its violations listed in `infeasible` |
| 400 | Field errors (RFC 7807), for example `criteria[0].weight` or `options[1].values.cost` |
| 401 / 503 | Missing or wrong key / no key configured |
| 413 | Body larger than 256 KB |
| 429 | More than 300 requests per minute from one client IP |

A well-formed `X-Correlation-ID` (8–64 letters, digits, `.`, `_` or `-`) is echoed and added to the log scope; otherwise a new one is returned.

**Example.**

```powershell
$body = @{
  question = "Which fix first?"
  criteria = @(@{ id = "impact"; weight = 3 }, @{ id = "effort"; weight = 1; direction = "minimize" })
  options  = @(@{ id = "a"; values = @{ impact = 10; effort = 5 } }, @{ id = "b"; values = @{ impact = 6; effort = 1 } })
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri https://localhost:7259/api/decisions/evaluate -ContentType application/json -Body $body `
  -Headers @{ "X-Api-Key" = $env:SMARTDOG_API_KEY; "X-Correlation-ID" = "example-0001" }
```

The response ranks `a` first with score 0.75 (impact 1 × 0.75 + effort 0 × 0.25) and `b` second with 0.25, each with its contributions and an explanation.

## Run and test

```powershell
dotnet run --project SmartDog.Api --launch-profile https   # https://localhost:7259
dotnet test SmartDog.slnx
```

The tests include API tests that host the real pipeline in memory with an EF Core in-memory database, so they need no SQL Server. They cover ownership for two users and an administrator on every decision endpoint, and the evaluation API's authentication, limits and contract. The evaluator is also compared with an independent decimal reference on 400 seeded random requests.

The API uses the Development launch profiles in `SmartDog.Api/Properties/launchSettings.json`. The health endpoint is `/health`; built-in OpenAPI is available in Development.
Login and registration requests are rate-limited per remote IP. If hosting behind a reverse proxy, configure forwarded headers only for explicitly trusted proxies before relying on the client-IP limit.

Run the Angular client in a second terminal:

```powershell
cd SmartDog.Client
npm ci
npm start
```

The client expects the API HTTPS profile on `https://localhost:7259`; the API development CORS policy permits `http://localhost:4200`. Client build, test, environment, and browser-token limitations are documented in `SmartDog.Client/README.md`.

## Database migrations

Migrations live in `SmartDog.Infrastructure/Migrations`. Create a migration with:

```powershell
dotnet ef migrations add AddYourMigrationName --project SmartDog.Infrastructure --startup-project SmartDog.Api
```

Review the generated migration and SQL before applying it. Apply only after verifying the active connection targets the intended database:

```powershell
dotnet ef database update --project SmartDog.Infrastructure --startup-project SmartDog.Api
```

To review the exact SQL first, generate an idempotent script, for example from `AddDecisionCriteriaAndRatings`:

```powershell
dotnet ef migrations script 20261010042047_AddDecisionCriteriaAndRatings AddDecisionOwnership --idempotent --project SmartDog.Infrastructure --startup-project SmartDog.Api -o AddDecisionOwnership.sql
```

`AddDecisionOwnership` adds a nullable `Decisions.OwnerId` (a foreign key to `Users`) and an index. It does not change existing rows. Existing decisions therefore have no owner and are hidden from everyone until you assign them, for example all of them to one account:

```sql
DECLARE @owner uniqueidentifier = (SELECT Id FROM dbo.Users WHERE Username = N'your-username');
IF @owner IS NULL THROW 50000, 'No such user.', 1;
UPDATE dbo.Decisions SET OwnerId = @owner WHERE OwnerId IS NULL;
```

Back up the database before applying migrations. To undo `AddDecisionOwnership`, update to `20261010042047_AddDecisionCriteriaAndRatings`, which drops the column, index and foreign key and leaves all other data unchanged.

## Continuous integration

GitHub Actions runs a .NET restore/build/test job and an Angular install/build/test/dependency-audit job for pushes to `main` and pull requests. It does not connect to a database, deploy the application, or modify Azure resources.

## Current scope

The API supports decisions owned by their creators, alternatives, authentication, criteria, alternative ratings, weighted results, status transitions, an optional AI assistant, and the stateless evaluation API for service clients. The Angular client supports these decision workflows. Azure deployment, production identity, and a production API reverse proxy are not configured in this repository.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
