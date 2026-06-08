# Architecture Notes

This is a generic learning project inspired by the structure of the KBKG services, not a copy of any business-sensitive implementation.

Patterns reused conceptually:

- Thin service startup with service registration in one place.
- API Gateway front door and service-specific downstream APIs.
- Shared response envelope, auth helpers, middleware, and event contracts.
- Tenant resolution before business endpoints execute.
- SQL Server for product data, MongoDB only for API/RabbitMQ audit logs.
- RabbitMQ-style domain events for notification/report/background work.
- Async report status tracking.

The first runnable version uses in-memory repositories and a mock event queue so it can compile without private NuGet feeds, external drivers, or real infrastructure. The SQL schema, Compose services, Ocelot-compatible routing file, and config sections are included as the intended production direction.
