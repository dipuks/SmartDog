# SmartDog

SmartDog is a .NET 10 decision-management application with an ASP.NET Core API and Angular client. The backend is organized into Domain, Application, Infrastructure, API, and Tests projects; it uses Entity Framework Core with SQL Server and JWT bearer authentication.

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

## Run and test

```powershell
dotnet run --project SmartDog.Api
dotnet test SmartDog.slnx
```

The API uses the Development launch profiles in `SmartDog.Api/Properties/launchSettings.json`. The health endpoint is `/health`; built-in OpenAPI is available in Development.
Login attempts are limited per remote IP. If hosting behind a reverse proxy, configure forwarded headers only for explicitly trusted proxies before relying on the client-IP limit.

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

The API supports decisions, alternatives, authentication, criteria, alternative ratings, weighted results, and status transitions. The Angular client supports these decision workflows. Azure deployment, production identity, and a production API reverse proxy are not configured in this repository.
