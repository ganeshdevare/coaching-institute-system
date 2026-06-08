# Architecture Notes

This is a generic learning project inspired by the structure of the KBKG services, not a copy of any business-sensitive implementation.

Patterns reused conceptually:

- Thin service startup with service registration in one place.
- Controller-service project slices with request models separated from HTTP actions.
- API Gateway front door and service-specific downstream APIs.
- Shared response envelope, auth helpers, middleware, and event contracts.
- Tenant resolution before business endpoints execute.
- SQL Server for product data, MongoDB only for API/RabbitMQ audit logs.
- RabbitMQ-style domain events for notification/report/background work.
- Async report status tracking.

The checked-in `appsettings.json` files use placeholders only. Runtime secrets should come from environment variables, user secrets, deployment secret stores, or environment-specific files that are not committed.

The first runnable version uses in-memory repositories and a mock event queue so it can compile without private NuGet feeds, external drivers, or real infrastructure. The production SQL Server schema is in `database/CoachInstituteManagementSystem_DDL.sql`, and it is the contract to use when replacing the learning store with SQL repositories.
