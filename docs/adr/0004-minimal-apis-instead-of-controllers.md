# Minimal APIs instead of controllers

The API is exposed through Minimal API endpoints, not MVC controllers. Every endpoint does the same three things: build a command or query, call its handler, and turn the `Result` into a response. A controller wraps that in a class, attributes and MVC machinery (model state, output formatters, `ProblemDetailsFactory`) that we don't use. Endpoints are grouped in one static class per resource, with named handler methods, so each endpoint's dependencies, route and response metadata sit in one place. New ASP.NET Core API features (built-in OpenAPI, validation, typed results) are designed for Minimal APIs first.

## Considered options

- **Keep controllers**: works, and gives automatic 400 responses for malformed bodies through `[ApiController]`. But that is the only MVC feature we rely on, and it is cheap to replace.
- **Generate endpoints from annotated service interfaces with a source generator**: one source of truth for routes, auth and response codes, and a typed C# client for free. Our only client is the Vue app, so the client half is unused, and the generator is a compiler project of its own to maintain.
- **FastEndpoints**: a framework with its own conventions (a class per endpoint, its own validation and pipeline). Another dependency with its own way of doing things that we'd need to learn.
- **Carter**: only adds module discovery, which is one `Map…Endpoints()` call per class when written by hand.

## Consequences

- Without `[ApiController]`, malformed bodies don't produce model-state errors. `ThrowOnBadRequest` is on in every environment and `GlobalExceptionHandler` answers `BadHttpRequestException` with a 400 (or 415) problem response, with the exception message as `detail` only in Development.
- OpenAPI response metadata is declared on every endpoint with `.Produces*`, and every endpoint has a unique `.WithName`, which is its `operationId` and the route name for `Location` headers.
- Sign-in is required on the `/api` group, not through a fallback policy, because a fallback policy would also cover the SPA fallback endpoint. Register and Login opt out with `AllowAnonymous`.
- Minimal APIs read `Microsoft.AspNetCore.Http.Json.JsonOptions` (`ConfigureHttpJsonOptions`), not the MVC JSON options.
- Turning a `Failure` into HTTP stays in one place (`FailureHttpExtensions.ToProblem`). Field errors keep the status of their failure kind, so it doesn't use `TypedResults.ValidationProblem`, which is always 400.
