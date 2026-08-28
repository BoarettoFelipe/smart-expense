# SmartExpense

SmartExpense is a full-stack personal finance application built with React,
ASP.NET Core, Entity Framework Core, and PostgreSQL.

## Prerequisites

- .NET 10 SDK
- Node.js 24 or another version supported by the current Vite release
- Docker Desktop with Docker Compose

## First-time local setup

1. Copy `.env.example` to `.env` and replace the local PostgreSQL password.
   Keep `.env` local; it is ignored by Git.
2. Start PostgreSQL only:

   ```shell
   docker compose up -d
   ```

3. Store the API's development secrets outside the repository. Replace the
   placeholders with your own local values:

   ```shell
   dotnet user-secrets set "Jwt:SigningKey" "<LOCAL_SIGNING_KEY_AT_LEAST_32_BYTES>" --project backend/src/SmartExpense.Api
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=smart_expense;Username=smart_expense;Password=<LOCAL_POSTGRES_PASSWORD>" --project backend/src/SmartExpense.Api
   ```

4. Restore the repository-local EF tool and apply migrations when needed:

   ```shell
   dotnet tool restore
   dotnet ef database update --project backend/src/SmartExpense.Infrastructure --startup-project backend/src/SmartExpense.Api
   ```

5. Start the API:

   ```shell
   dotnet run --project backend/src/SmartExpense.Api
   ```

6. In another terminal, install frontend dependencies and start Vite:

   ```shell
   cd frontend
   npm ci
   npm run dev
   ```

Vite keeps its development proxy and sends relative `/api` requests to
`http://localhost:5239` by default. Copy `frontend/.env.example` to
`frontend/.env.local` only when that host API address needs to change.

## Normal host development

After the first-time setup, the usual host workflow is:

```shell
docker compose up -d
dotnet run --project backend/src/SmartExpense.Api
```

Then run `npm run dev` from `frontend` in another terminal. The root Compose
command intentionally starts only PostgreSQL.

## Full Docker stack

Set a unique `POSTGRES_PASSWORD` and `JWT_SIGNING_KEY` in the root `.env`, then
build and start PostgreSQL, the one-shot migration service, API, and frontend:

```shell
docker compose --profile app up --build
```

Open [http://localhost:8080](http://localhost:8080). nginx serves the React SPA
and proxies browser `/api` requests to the internal API service, so normal
containerized browser traffic does not require CORS configuration.

The root `.env` values are server-side Docker Compose configuration. They are
not browser-visible Vite variables. Variables prefixed with `VITE_` are bundled
into frontend code and must never contain secrets.

Stop the full stack without deleting PostgreSQL data:

```shell
docker compose --profile app down
```

`docker compose down -v` also deletes the PostgreSQL volume and its data. Use it
only when a destructive database reset is intentional, not during normal
development.

## Continuous integration

The GitHub Actions CI workflow runs for pull requests targeting `main` and
pushes to `main`. It validates the Release backend build and complete test suite,
frontend lint and production build, and an isolated full-stack Docker smoke test.
CI uses disposable local-only database and JWT values and does not deploy or
publish container images.
