# CQRS without a mediator, one transaction per command

The backend splits use cases into commands and queries, each with its own handler. The split is logical only: one Postgres database, one `CboDbContext`, no events, no separate read store. Endpoints get the handler they need by dependency injection (`ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`). No mediator or dispatcher sits in between. Every command handler derives from one base class that opens a transaction, runs the handler's `ExecuteAsync`, calls `SaveChangesAsync` once and commits. A `Failure` or an exception rolls everything back. With a single context, a single client (our Vue app) and endpoints that already know which use case they call, a dispatcher adds only indirection: a runtime type lookup instead of a compile-time check, and every handler built on every request.

## Considered options

- **MediatR**: a third-party dependency (recent versions are commercially licensed) whose main value is pipeline behaviors and decoupled dispatch. We need neither.
- **A hand-written typed sender** (`ISender.SendAsync<TResult>(ICommand<TResult>)`): basically a small MediatR. It's only worth writing if we ever need cross-cutting behaviors that a base class can't express.
- **Decorators for the commit**: Microsoft DI has no decorator support, so this needs Scrutor or hand-written registration for every handler.
- **Step hooks in the base class** (`Initialize → Validate → Execute → Finalize`, as in the reference project): cbo's use cases order their checks differently (access, then validation, then state), and the status code depends on that order. Handlers therefore run their checks in their own order inside one `ExecuteAsync`.

## Consequences

- Repositories go away. Handlers use `CboDbContext` directly; queries project straight to DTOs.
- Nothing except the base class calls `SaveChangesAsync`. `ExecuteDelete` and `ExecuteUpdate` are atomic with the save, because both run inside the command's transaction.
- A unique or foreign-key violation can only surface in the base class, so handlers translate expected violations through an overridable `MapSaveFailure`. Everything else is a 500.
- Commands don't return DTOs. When an endpoint has to return the resource, it runs the matching query after the command.
- A handler never calls another handler; shared logic lives in plain services.
- Auth stays a service, because ASP.NET Core Identity's `UserManager` saves on its own.
