# SmartDog Client

Strict-mode Angular client for the SmartDog API. It includes sign-in and account registration, protected decision pages, decision filtering/sorting/pagination, decision status and description updates, alternative and criterion management, ratings, weighted results, and an optional AI decision assistant. Users can enter their own OpenAI API key in the assistant UI. The key stays in client memory only and is sent to the API over HTTPS with assistant requests; it is not persisted in browser storage. See the repository root README for data handling.

## Prerequisites

- Node.js 24.15.0 or later in the Node 24 line
- npm
- SmartDog API running with a development signing key configured as described in the repository root README

## Run locally

```powershell
npm ci
npm start
```

The development environment sends API requests to `https://localhost:7259/api`; run the API using its HTTPS launch profile. The API's development CORS policy allows `http://localhost:4200`. If the local ASP.NET development certificate is not trusted, trust it with `dotnet dev-certs https --trust`.

## Build and test

```powershell
npm run build
npm test -- --watch=false
```

The production build uses `/api` as its API base URL and therefore expects a same-origin reverse proxy or host to route `/api` requests to the backend. No production host or proxy is configured here.

## Authentication limitation

The demonstration API returns a bearer token, which this client keeps in browser `sessionStorage`. This limits persistence to the browser tab's session, but it does not protect the token from JavaScript running in the page. Use a trusted identity provider and a backend-for-frontend with secure, HTTP-only cookies for production rather than treating this local JWT flow as production-grade authentication.
