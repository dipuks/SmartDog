# SmartDog

SmartDog helps people make considered choices by comparing alternatives against weighted criteria. Create a decision, list the options, choose what matters, rate each option, and review a transparent weighted ranking. An optional AI assistant can suggest criteria and alternatives or explain the calculated results without changing the user's ratings or decision.

The repository contains an ASP.NET Core REST API and an Angular single-page client. Users can register and sign in, create and manage decisions, compare alternatives, and view ranked results. The API uses SQL Server persistence and JWT bearer authentication. The optional OpenAI assistant accepts a user-provided key in the UI; the key is not saved by the app.

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

## AI decision assistant

The authenticated decision workspace includes an optional OpenAI-powered assistant. Users can enter their own key in the assistant UI; no provider key is embedded in the app or required on the server. The key is held only in client memory, sent to the API over HTTPS for assistant requests, and cleared on refresh, tab close, or sign-out. It is not written to browser storage or application logs. Assistant calls are limited to 20 per authenticated user per hour in the API process.

When a user asks the assistant, the API forwards that decision's title, description, criteria and weights, alternatives and descriptions, ratings, calculated ranking when complete, and the user's question to OpenAI. Do not include sensitive information in decision data unless you are comfortable sending it to OpenAI. Responses can be incorrect and should be treated as suggestions, not advice.

## Run and test

```powershell
dotnet run --project SmartDog.Api
dotnet test SmartDog.slnx
```

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

## Continuous integration

GitHub Actions runs a .NET restore/build/test job and an Angular install/build/test/dependency-audit job for pushes to `main` and pull requests. It does not connect to a database, deploy the application, or modify Azure resources.

## Current scope

The API supports decisions, alternatives, authentication, criteria, alternative ratings, weighted results, status transitions, and an optional AI assistant. The Angular client supports these decision workflows. Azure deployment, production identity, and a production API reverse proxy are not configured in this repository.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
