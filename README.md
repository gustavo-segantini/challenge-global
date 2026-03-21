# Devices API

A production-ready REST API built with C# and .NET for managing device resources with business-rule enforcement and relational database persistence.

## Project Description

Devices API manages `Device` resources with the following fields:

- `Id` (unique identifier)
- `Name`
- `Brand`
- `State` (`available`, `in-use`, `inactive`)
- `CreationTime` (creation timestamp)

The API enforces domain rules at the domain/application layers:

- `CreationTime` is immutable after creation.
- When state is `in-use`, `Name` and `Brand` cannot be changed.
- Devices in state `in-use` cannot be deleted.

## Tech Stack

- .NET 10 SDK (compatible with .NET 9+ requirement)
- C# 13+ language features
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL (Npgsql provider)
- FluentValidation
- Swagger / OpenAPI (Swashbuckle)
- xUnit, Moq, FluentAssertions
- Docker and Docker Compose

## Solution Architecture

The solution follows a clean layered architecture:

- `src/Devices.Api`
  - HTTP layer (controllers, middleware, startup configuration)
  - Swagger/OpenAPI exposure
  - Global exception handling and HTTP status mapping
- `src/Devices.Application`
  - Use-case orchestration (`IDeviceService`, `DeviceService`)
  - DTO contracts and mapping from domain entities
  - Application exceptions (`NotFound`, `Validation`, `Conflict`)
- `src/Devices.Domain`
  - Core entity (`Device`) and domain rules
  - State enum and parsing helpers
  - Domain rule exception
- `src/Devices.Infrastructure`
  - EF Core `DbContext`
  - Repository implementation
  - Database provider and DI wiring
  - Migrations
- `tests/Devices.UnitTests`
  - Domain and service unit tests
- `tests/Devices.IntegrationTests`
  - API integration tests using in-memory SQLite host override

## Suggested Commit Breakdown

A clean commit strategy for this repository:

1. `chore: scaffold solution and projects`
2. `feat: implement domain and application layers`
3. `feat: implement infrastructure persistence and migrations`
4. `feat: add REST controllers, swagger, and exception middleware`
5. `test: add unit and integration tests`
6. `chore: add docker artifacts and readme`

## Mini Branch Flow (Standard)

Use this lightweight flow for all new work:

1. Create feature branch from `develop`:

```bash
git checkout develop
git pull origin develop
git checkout -b feature/<short-name>
```

2. Commit and push your feature branch:

```bash
git add .
git commit -m "feat: <what changed>"
git push origin feature/<short-name>
```

3. Open Pull Request: `feature/<short-name>` -> `develop`.

4. Release flow: open Pull Request `develop` -> `main`.

5. After release merge, create and push a tag from `main`:

```bash
git checkout main
git pull origin main
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

## API Endpoints

Base route (v1): `/api/v1/devices`

1. `POST /api/v1/devices`
2. `PUT /api/v1/devices/{id}`
3. `PATCH /api/v1/devices/{id}`
4. `GET /api/v1/devices/{id}`
5. `GET /api/v1/devices?pageNumber=1&pageSize=20`
6. `GET /api/v1/devices/brand/{brand}`
7. `GET /api/v1/devices/state/{state}`
8. `DELETE /api/v1/devices/{id}`

Versioning is configured with a default API version of `1.0`. You can specify version via:

- URL segment (preferred): `/api/v1/...`
- Query string: `?api-version=1.0`
- Header: `x-api-version: 1.0`

## Health Endpoints

- `GET /health/live`: process liveness probe
- `GET /health/ready`: readiness probe including database connectivity

## Sample Requests and Responses

### Create Device

Request:

```http
POST /api/v1/devices
Content-Type: application/json

{
  "name": "Edge Router",
  "brand": "Fabrikam",
  "state": "available"
}
```

Response (201 Created):

```json
{
  "id": "b7eb1f92-70e8-4edd-a81f-5f7d7830a83c",
  "name": "Edge Router",
  "brand": "Fabrikam",
  "state": "available",
  "creationTime": "2026-03-20T13:30:04.0857562+00:00"
}
```

### Full Update Device

```http
PUT /api/v1/devices/{id}
Content-Type: application/json

{
  "name": "Edge Router X",
  "brand": "Fabrikam",
  "state": "inactive"
}
```

### Partial Update Device

```http
PATCH /api/v1/devices/{id}
Content-Type: application/json

{
  "state": "in-use"
}
```

### Validation / Business Rule Error

Response (409 Conflict):

```json
{
  "type": "about:blank",
  "title": "Business rule conflict",
  "status": 409,
  "detail": "Devices in state 'in-use' cannot be deleted.",
  "instance": "/api/devices/{id}",
  "traceId": "00-..."
}
```

### Error Contract

The API returns RFC7807-compatible error payloads (`ProblemDetails` / `ValidationProblemDetails`) with:

- `status`, `title`, `detail`, `instance`, `type`
- `traceId` extension for request correlation
- `errorCode` extension for machine-readable handling
- `timestampUtc` extension for observability

Exception mapping includes:

- `400` for validation errors (`FluentValidation` and request validation)
- `404` for missing resources
- `409` for business rule or persistence conflicts
- `503/504` for timeout/dependency-related failures

## Database and Migrations

Connection string can be provided via either:

- `ConnectionStrings:DevicesDb`
- `DEVICES_DB_CONNECTION_STRING`

Create migration (already included in repository):

```bash
dotnet ef migrations add InitialCreate \
  --project src/Devices.Infrastructure/Devices.Infrastructure.csproj \
  --startup-project src/Devices.Api/Devices.Api.csproj \
  --output-dir Persistence/Migrations
```

Apply migration manually if needed:

```bash
dotnet ef database update \
  --project src/Devices.Infrastructure/Devices.Infrastructure.csproj \
  --startup-project src/Devices.Api/Devices.Api.csproj
```

The API also applies migrations automatically at startup (except test environment).

## How To Run Locally

### Prerequisites

- .NET SDK 10.0+ (or SDK that supports .NET 9+ targets)
- PostgreSQL running locally

### Run

```bash
dotnet restore

dotnet run --project src/Devices.Api/Devices.Api.csproj
```

Swagger UI will be available at:

- `http://localhost:5000/swagger` or `https://localhost:5001/swagger` depending on local profile

If needed, override connection string:

```bash
# PowerShell
$env:ConnectionStrings__DevicesDb="Host=localhost;Port=5432;Database=devicesdb;Username=postgres;Password=postgres"
```

## How To Run Tests

```bash
dotnet test DevicesApi.sln
```

## Run With Docker

### API + PostgreSQL with Docker Compose

```bash
docker compose up --build
```

API endpoint:

- `http://localhost:8080/swagger`

### API container only

```bash
docker build -t devices-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__DevicesDb="Host=<db-host>;Port=5432;Database=devicesdb;Username=postgres;Password=postgres" \
  devices-api
```

## Known Limitations

- Authentication and authorization are not implemented.
- Filtering by brand is exact match only.
- No caching layer is enabled.

## Future Improvements

1. Add authentication/authorization (JWT + role policies).
2. Add structured logging and distributed tracing (OpenTelemetry).
3. Add richer querying (sorting, filtering, search, cursor pagination).
4. Add optimistic concurrency with row versioning.
5. Add CI pipeline for linting, tests, and container scanning.
