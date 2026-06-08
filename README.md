# Coaching Institute Management System

Minimal architecture-rich multi-tenant SaaS backend for revision and learning.

## Services

- `ApiGateway`: front door with local reverse proxy and Ocelot-compatible route config.
- `IdentityService`: institute registration, subdomain ownership, login, JWT, refresh tokens, invitations, password flows, audit search.
- `InstituteService`: branches, courses, batches, teacher assignment, enrollment, guardian mapping, attendance, dashboard.
- `BillingService`: fee plans, payments, partial payments, refunds, credit balance, receipt PDF, async reports, mock payment webhook idempotency.
- `NotificationWorker`: mock consumer for payment, refund, low attendance, fee reminder, and report events.
- `SharedKernel`: response models, tenant/correlation/audit middleware, JWT utilities, event contracts, in-memory repository.

## Local URLs

- Gateway: `http://localhost:5100`
- Identity health: `http://localhost:5101/health`
- Institute health: `http://localhost:5102/health`
- Billing health: `http://localhost:5103/health`
- RabbitMQ management: `http://localhost:15673`
- SQL Server: `localhost,14333`
- MongoDB: `localhost:27018`
- Redis: `localhost:6380`

## Run Locally

```powershell
dotnet restore .\CoachingInstituteSystem.slnx --configfile .\nuget.config
dotnet build .\CoachingInstituteSystem.slnx

dotnet run --project .\src\IdentityService\IdentityService.csproj --urls http://localhost:5101
dotnet run --project .\src\InstituteService\InstituteService.csproj --urls http://localhost:5102
dotnet run --project .\src\BillingService\BillingService.csproj --urls http://localhost:5103
dotnet run --project .\src\ApiGateway\ApiGateway.csproj --urls http://localhost:5100
dotnet run --project .\src\NotificationWorker\NotificationWorker.csproj
```

For infrastructure:

```powershell
docker compose up -d sqlserver mongodb redis rabbitmq
```

The current code runs in memory. Apply `database/001_schema.sql` and `database/002_seed.sql` when you replace repositories with SQL Server-backed implementations.

## Seed Login

- Email: `superadmin@coachapp.local`
- Password: `Admin@123`

## Sample Flow

1. Register an institute:

```http
POST http://localhost:5101/api/v1/identity/institutes/register
Content-Type: application/json

{
  "name": "Bright Classes",
  "subdomain": "brightclasses",
  "ownerEmail": "owner@brightclasses.local",
  "ownerName": "Bright Owner",
  "password": "Owner@123"
}
```

2. Login as the institute owner:

```http
POST http://localhost:5101/api/v1/identity/login
Content-Type: application/json

{
  "email": "owner@brightclasses.local",
  "password": "Owner@123"
}
```

3. Use tenant-aware endpoints with:

```http
X-Tenant-Subdomain: brightclasses
Authorization: Bearer <accessToken>
```

4. Create courses, batches, students, fee plans, payments, attendance, and reports.

## Data Architecture

Product data belongs in SQL Server. The schema includes tenant foreign keys, unique subdomain enforcement, tenant-scoped indexes, payment idempotency, constraints, and triggers for selected cross-tenant safety checks.

MongoDB is reserved for audit/event logs only. This mirrors the requested separation: no product data is stored in MongoDB.

## Reference Boundary

Existing KBKG repos were inspected only for architectural shape: service folders, startup composition, gateway routing, shared core patterns, audit/logging concepts, RabbitMQ/event background work, and SQL script organization. No secrets or business-sensitive code were copied.
